using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Comparers;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkSubsidiaryProcessor(ISubsidiaryService organisationService, ICompaniesHouseDataProvider companiesHouseDataProvider, ILogger<BulkSubsidiaryProcessor> logger, INotificationService notificationService)
    : IBulkSubsidiaryProcessor
{
    private readonly ILogger<BulkSubsidiaryProcessor> _logger = logger;
    private readonly ISubsidiaryService organisationService = organisationService;
    private readonly ICompaniesHouseDataProvider companiesHouseDataProvider = companiesHouseDataProvider;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<int> Process(IEnumerable<CompaniesHouseCompany> subsidiaries, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg, UserRequestModel userRequestModel)
    {
        IEnumerable<CompaniesHouseCompany> franchiseeProcessed = [];
        var companiesWithFranchiseeFlagRecords = subsidiaries.Count(ch => ch.franchisee_licensee_tenant == "Y" && (ch.Errors == null || ch.Errors.Count == 0));
        if (companiesWithFranchiseeFlagRecords > 0)
        {
            franchiseeProcessed = await ProcessFranchisee(subsidiaries, parentOrg, userRequestModel);
            if (companiesWithFranchiseeFlagRecords == subsidiaries.Count())
            {
                return franchiseeProcessed.Count();
            }
        }

        // remove all franchisee from the main collection : subsidiaries
        var nonFranchiseeSubsidiaries = subsidiaries.Except(subsidiaries.Where(ch => ch.franchisee_licensee_tenant == "Y" && (ch.Errors == null || ch.Errors.Count == 0)));

        var nonNullCompaniesHouseNumberRecords = nonFranchiseeSubsidiaries.Where(ch => !string.IsNullOrEmpty(ch.companies_house_number));

        var subsidiariesAndOrg = nonNullCompaniesHouseNumberRecords
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.companies_house_number)));

        var subsidiariesAndOrgWithValidNameProcessStatistics = await ProcessValidNamedOrgs(subsidiariesAndOrg, parentOrg, userRequestModel);

        var subsidiariesAndOrgWithMatchingReportingType = subsidiariesAndOrg
        .Where(sub => sub.SubsidiaryOrg != null && sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber
        && !string.Equals(sub.Subsidiary.reporting_type, ((ReportingType)sub.SubsidiaryOrg.OrganisationRelationship.ReportingTypeId).ToString(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg.name, StringComparison.OrdinalIgnoreCase)
        && parentOrg.id == sub.SubsidiaryOrg.OrganisationRelationship?.FirstOrganisationId);

        var subsidiariesAndOrgWithValidNameanJointerDateProcessStatistics = await ProcessValidNamedOrgsUpdate(subsidiariesAndOrg, parentOrg, userRequestModel);

        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg.Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber
            && !string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg?.name, StringComparison.OrdinalIgnoreCase));
        var subWithInvalidName = await subsidiariesAndOrgWith_InValidName.Select(s => s.Subsidiary).ToListAsync();

        /*Scenario 2: The subsidiary found in RPD. name not match*/
        await ReportCompanies(subWithInvalidName, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInRPDMessage, BulkUpdateErrors.CompanyNameIsDifferentInRPD);

        var subsidiariesAndOrgWith_InValidNameAndJoinerDate = subsidiariesAndOrg.Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber
            && !string.Equals(sub.Subsidiary.joiner_date, sub.SubsidiaryOrg.OrganisationRelationship?.JoinerDate?.ToString("dd/MM/yyyy"), StringComparison.InvariantCulture));
        var subWithInvalidNameAndJoinerDate = await subsidiariesAndOrgWith_InValidNameAndJoinerDate.Select(s => s.Subsidiary).ToListAsync();

        /*Scenario x: The subsidiary found in RPD. joiner date not match*/
        await ReportCompanies(subWithInvalidNameAndJoinerDate, userRequestModel, BulkUpdateErrors.JoinerDateInvalidMessage, BulkUpdateErrors.JoinerDateInvalid);

        var remainingToProcess = nonNullCompaniesHouseNumberRecords.Except(subWithInvalidName)
            .Except(subWithInvalidNameAndJoinerDate)
            .Except(subsidiariesAndOrgWithValidNameProcessStatistics.NewAddedSubsidiaries)
            .Except(subsidiariesAndOrgWithValidNameProcessStatistics.AlreadyExistCompanies)
            .Except(subsidiariesAndOrgWithValidNameanJointerDateProcessStatistics.UpdatedAddedSubsidiaries);

        /*Scenario 3: The subsidiary found in Offline data. name matches then Add OR name not match then get it from CH API and name matches with CH API data.*/
        var newSubsidiariesToAdd_DataFromLocalStorageOrCH = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
        .SelectAwait(async subsidiary =>
            (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userRequestModel.UserId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        var companyHouseAPIProcessStatistics = await ProcessCompanyHouseAPI(newSubsidiariesToAdd_DataFromLocalStorageOrCH, userRequestModel);
        var remainingToProcessAfterAPIChecks = remainingToProcess.Except(companyHouseAPIProcessStatistics.CompaniesHouseAPIErrorListReported);
        var remainingToProcessAfterLocalSubAdditions = remainingToProcessAfterAPIChecks.Except(companyHouseAPIProcessStatistics.NewAddedSubsidiaries).Except(companyHouseAPIProcessStatistics.DuplicateSubsidiaries).Except(companyHouseAPIProcessStatistics.NotAddedSubsidiaries);

        /*Scenario 4: The subsidiary found in Offline data. name not match. get it from CH API and name not matches with CH API data. Report Error.*/
        var newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouse_NameNoMatch = newSubsidiariesToAdd_DataFromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error == null
            && !string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.CompaniesHouseCompanyName, StringComparison.OrdinalIgnoreCase));

        var newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList = await newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouse_NameNoMatch.Select(s => s.Subsidiary).ToListAsync();
        await ReportCompanies(newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPIMessage, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPI);

        var remainingAfterProcessingAllData = remainingToProcessAfterLocalSubAdditions.Except(newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList);

        var allAddedNewSubsPlusExisting = await newSubsidiariesToAdd_DataFromLocalStorageOrCH.Where(sta => sta.LinkModel.StatusCode == System.Net.HttpStatusCode.OK).Select(sta => sta.Subsidiary)
            .ToListAsync();

        /*Scenario 1: The subsidiary is not found in RPD and not in Local storage and not found on companies house*/
        await ReportCompanies(remainingAfterProcessingAllData.Except(allAddedNewSubsPlusExisting), userRequestModel, BulkUpdateErrors.CompanyNameNotFoundAnywhereMessage, BulkUpdateErrors.CompanyNameNotFoundAnywhere);

        return allAddedNewSubsPlusExisting.Count + franchiseeProcessed.Count() + subsidiariesAndOrgWithValidNameProcessStatistics.NewAddedSubsidiariesRelationships + companyHouseAPIProcessStatistics.NewAddedSubsidiaries.Count;
    }

    private async Task ReportCompanies(IEnumerable<LinkOrganisationModel> linkSubsidiaries, UserRequestModel userRequestModel)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in linkSubsidiaries)
        {
            notificationErrorList.Add(company.Subsidiary.Error);
        }

        if (notificationErrorList.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);
        await _notificationService.SetStatus(key, "Error Reporting.");
        await _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation(BulkUpdateErrors.ResourceNotReachableOrAllOtherPossibleErrorMessage);
    }

    private async Task ReportCompanies(IEnumerable<CompaniesHouseCompany> subsidiaries, UserRequestModel userRequestModel, string errorMessage, int errorNumber)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in subsidiaries)
        {
            var newError = new UploadFileErrorModel()
            {
                FileLineNumber = company.FileLineNumber,
                FileContent = company.RawRow,
                Message = errorMessage,
                IsError = true,
                ErrorNumber = errorNumber
            };

            notificationErrorList.Add(newError);
        }

        if (notificationErrorList.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);
        await _notificationService.SetStatus(key, "Started reporting invalid subsidiaries.");
        await _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation("{ErrorMessage}", errorMessage);
    }

    private async Task<LinkOrganisationModel?> GetLinkModelForCompaniesHouseData(CompaniesHouseCompany subsidiary, OrganisationResponseModel parentOrg, Guid userId)
    {
        var reportingTypeEnum = Enum.TryParse<ReportingType>(subsidiary.reporting_type, true, out var reportingType) ? reportingType : (ReportingType?)null;

        var newSubsidiaryModel = new LinkOrganisationModel()
        {
            UserId = userId,
            Subsidiary = new OrganisationModel()
            {
                ReferenceNumber = subsidiary.organisation_id,
                Name = subsidiary.organisation_name,
                CompaniesHouseNumber = subsidiary.companies_house_number,
                OrganisationType = OrganisationType.CompaniesHouseCompany,
                ProducerType = ProducerType.Other,
                IsComplianceScheme = false,
                Nation = Nation.NotSet,
                SubsidiaryOrganisationId = subsidiary.subsidiary_id,
                RawContent = subsidiary.RawRow,
                FileLineNumber = subsidiary.FileLineNumber,
                JoinerDate = subsidiary.joiner_date,
                ReportingTypeId = (int?)reportingTypeEnum
            },
            ParentOrganisationId = parentOrg.ExternalId.Value
        };

        var modelLoaded = await companiesHouseDataProvider.SetCompaniesHouseData(newSubsidiaryModel.Subsidiary);

        return modelLoaded ? newSubsidiaryModel : null;
    }

    private async Task<HttpStatusCode> AddSubsidiary(OrganisationResponseModel parent, OrganisationResponseModel subsidiary, CompaniesHouseCompany subsidiaryInput, Guid userId)
    {
        var reportingTypeEnum = Enum.TryParse<ReportingType>(subsidiaryInput.reporting_type, true, out var reportingType) ? reportingType : (ReportingType?)null;

        var subsidiaryModel = new SubsidiaryAddModel
        {
            UserId = userId,
            ParentOrganisationId = parent.referenceNumber,
            ChildOrganisationId = subsidiary.referenceNumber,
            ParentOrganisationExternalId = parent.ExternalId,
            ChildOrganisationExternalId = subsidiary.ExternalId,
            JoinerDate = subsidiaryInput.joiner_date,
            ReportingTypeId = (int?)reportingTypeEnum
        };
        var result = await organisationService.AddSubsidiaryRelationshipAsync(subsidiaryModel);

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);

        return result;
    }

    private async Task<HttpStatusCode> UpdateSubsidiaryRelationship(OrganisationResponseModel parent, OrganisationResponseModel subsidiaryOrg, CompaniesHouseCompany subsidiaryInput, Guid userId)
    {
        var reportingTypeEnum = Enum.TryParse<ReportingType>(subsidiaryInput.reporting_type, true, out var reportingType) ? reportingType : (ReportingType?)null;

        var subsidiaryModel = new SubsidiaryAddModel
        {
            UserId = userId,
            ParentOrganisationId = parent.referenceNumber,
            ChildOrganisationId = subsidiaryOrg.referenceNumber,
            ParentOrganisationExternalId = parent.ExternalId,
            ChildOrganisationExternalId = subsidiaryOrg.ExternalId,
            JoinerDate = subsidiaryOrg.joinerDate,
            ReportingTypeId = (int?)reportingTypeEnum
        };
        var result = await organisationService.UpdateSubsidiaryRelationshipAsync(subsidiaryModel);

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiaryOrg.referenceNumber, subsidiaryOrg.name, parent.referenceNumber);

        return result;
    }

    private async Task<IEnumerable<CompaniesHouseCompany>> ProcessFranchisee(IEnumerable<CompaniesHouseCompany> subsidiaries, OrganisationResponseModel parentOrg, UserRequestModel userRequestModel)
    {
        // companies with franchisee flag.
        var companiesWithFranchiseeFlagRecords = subsidiaries.Where(ch => ch.franchisee_licensee_tenant == "Y")
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompanyName(subsidiary.organisation_name)));

        // check if the incoming file company name is matching with the one in db. name to match.
        var subsidiariesAndOrgExistsInTheDB = companiesWithFranchiseeFlagRecords
            .Where(sub => sub.SubsidiaryOrg != null
            && NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg.name)
            && NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.Equals(sub.Subsidiary.companies_house_number, sub.SubsidiaryOrg.companiesHouseNumber));

        var knownFranchiseeToAddRelationship = subsidiariesAndOrgExistsInTheDB.Where(co => co.SubsidiaryOrg != null)
          .SelectAwait(async co =>
              (Subsidiary: co.Subsidiary,
               SubsidiaryOrg: co.SubsidiaryOrg,
               RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id)))
          .Where(co => !co.RelationshipExists);

        var knownFranchiseeToAddRelationshipToDB = await knownFranchiseeToAddRelationship.ToListAsync();

        foreach (var subsidiaryAddModel in knownFranchiseeToAddRelationshipToDB)
        {
            var status = await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, subsidiaryAddModel!.Subsidiary, userRequestModel.UserId);
            subsidiaryAddModel.Subsidiary.StatusCode = status;
        }

        var subsidiariesAndOrgNonExistsInTheDB = await companiesWithFranchiseeFlagRecords.Where(co => co.SubsidiaryOrg == null).ToListAsync();

        foreach (var subsidiaryAddModel in subsidiariesAndOrgNonExistsInTheDB)
        {
            var franchisee = new LinkOrganisationModel()
            {
                UserId = userRequestModel.UserId,
                Subsidiary = new OrganisationModel()
                {
                    ReferenceNumber = subsidiaryAddModel.Subsidiary.organisation_id,
                    Name = subsidiaryAddModel.Subsidiary.organisation_name,
                    CompaniesHouseNumber = subsidiaryAddModel.Subsidiary.companies_house_number,
                    OrganisationType = OrganisationType.NonCompaniesHouseCompany,
                    ProducerType = ProducerType.Other,
                    IsComplianceScheme = false,
                    Nation = Nation.NotSet,
                    SubsidiaryOrganisationId = subsidiaryAddModel.Subsidiary.subsidiary_id,
                    RawContent = subsidiaryAddModel.Subsidiary.RawRow,
                    FileLineNumber = subsidiaryAddModel.Subsidiary.FileLineNumber,
                    Franchisee_Licensee_Tenant = subsidiaryAddModel.Subsidiary.franchisee_licensee_tenant,
                    Address = new AddressModel()
                },
                ParentOrganisationId = parentOrg.ExternalId.Value
            };

            subsidiaryAddModel.Subsidiary.StatusCode = await organisationService.CreateAndAddSubsidiaryAsync(franchisee);
        }

        var knowFranchiseeRelationshipsAdded = knownFranchiseeToAddRelationshipToDB.Where(s => s.Subsidiary.StatusCode == System.Net.HttpStatusCode.OK).Select(s => s.Subsidiary).ToList();
        var subsNewlyAdded = subsidiariesAndOrgNonExistsInTheDB.Where(s => s.Subsidiary.StatusCode == System.Net.HttpStatusCode.OK).Select(s => s.Subsidiary).ToList();
        return subsNewlyAdded.Concat(knowFranchiseeRelationshipsAdded);
    }

    private async Task<AddSubsidiariesFigures> ProcessCompanyHouseAPI(IAsyncEnumerable<(CompaniesHouseCompany Subsidiary, LinkOrganisationModel LinkModel)> newSubsidiariesToAdd_DataFromLocalStorageOrCH, UserRequestModel userRequestModel)
    {
        /*Scenario : Companies house API Errors*/
        var counts = new AddSubsidiariesFigures();

        var companiesHouseAPIErrorList = await newSubsidiariesToAdd_DataFromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null
            && subAndLink.LinkModel.Subsidiary.Error != null).Select(s => s.LinkModel).ToListAsync();

        var companiesHouseCompanyAPIErrorList = await newSubsidiariesToAdd_DataFromLocalStorageOrCH
           .Where(subAndLink => subAndLink.LinkModel != null
           && subAndLink.LinkModel.Subsidiary.Error != null).Select(s => s.Subsidiary).ToListAsync();

        await ReportCompanies(companiesHouseAPIErrorList, userRequestModel);

        counts.CompaniesHouseAPIErrorListReported = companiesHouseCompanyAPIErrorList;

        var newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouseWithNameMatch = await newSubsidiariesToAdd_DataFromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error == null &&
            (string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.LocalStorageName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.CompaniesHouseCompanyName, StringComparison.OrdinalIgnoreCase)))
            .ToListAsync();

        foreach (var subsidiaryAndLink in newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouseWithNameMatch)
        {
            subsidiaryAndLink.LinkModel.StatusCode = await organisationService.CreateAndAddSubsidiaryAsync(subsidiaryAndLink.LinkModel);

            if (subsidiaryAndLink.LinkModel.StatusCode == System.Net.HttpStatusCode.OK)
            {
                counts.NewAddedSubsidiaries.Add(subsidiaryAndLink.Subsidiary);
            }
            else
            {
                counts.NotAddedSubsidiaries.Add(subsidiaryAndLink.Subsidiary);
            }
        }

        return counts;
    }

    private async Task<AddSubsidiariesFigures> ProcessValidNamedOrgs(IAsyncEnumerable<(CompaniesHouseCompany Subsidiary, OrganisationResponseModel SubsidiaryOrg)> subsidiariesAndOrgWithValidName, OrganisationResponseModel parentOrg, UserRequestModel userRequestModel)
    {
        var counts = new AddSubsidiariesFigures();
        var count = 0;

        var knownSubsidiariesToAddCheck = await subsidiariesAndOrgWithValidName.Where(co => co.SubsidiaryOrg != null)
        .SelectAwait(async co =>
            (Subsidiary: co.Subsidiary,
             SubsidiaryOrg: co.SubsidiaryOrg,
             RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id))).ToListAsync();

        var knownSubsidiariesToAdd = knownSubsidiariesToAddCheck.Where(co => !co.RelationshipExists);
        counts.SubsidiaryWithExistingRelationships = knownSubsidiariesToAddCheck.Where(co => co.RelationshipExists).Select(org => org.Subsidiary).ToList();
        counts.SubsidiariesWithNoExistingRelationships = knownSubsidiariesToAddCheck.Where(co => !co.RelationshipExists).Select(org => org.Subsidiary).ToList();

        foreach (var subsidiaryAddModel in knownSubsidiariesToAdd)
        {
            var result = await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, subsidiaryAddModel!.Subsidiary, userRequestModel.UserId);
            count = count + (result == HttpStatusCode.OK ? 1 : 0);
            counts.NewAddedSubsidiaries.Add(subsidiaryAddModel.Subsidiary);
        }

        counts.AlreadyExistCompanies = knownSubsidiariesToAddCheck.Select(org => org.Subsidiary).ToList();
        counts.NewAddedSubsidiariesRelationships = count;
        return counts;
    }

    private async Task<AddSubsidiariesFigures> ProcessValidNamedOrgsUpdate(IAsyncEnumerable<(CompaniesHouseCompany Subsidiary, OrganisationResponseModel SubsidiaryOrg)> subsidiariesAndOrgWithValidName, OrganisationResponseModel parentOrg, UserRequestModel userRequestModel)
    {
        var counts = new AddSubsidiariesFigures();
        counts.UpdatedAddedSubsidiaries = new List<CompaniesHouseCompany>();
        var count = 0;

        var knownSubsidiariesToUpdateCheck = await subsidiariesAndOrgWithValidName.Where(co => co.SubsidiaryOrg != null)
        .SelectAwait(async co =>
            (Subsidiary: co.Subsidiary,
             SubsidiaryOrg: co.SubsidiaryOrg,
             RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id))).ToListAsync();

        var knownSubsidiariesToUpdate = knownSubsidiariesToUpdateCheck.Where(co => co.RelationshipExists);
        foreach (var subsidiaryAddModel in knownSubsidiariesToUpdate)
        {
            var result = await UpdateSubsidiaryRelationship(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, subsidiaryAddModel!.Subsidiary, userRequestModel.UserId);
            count = count + (result == HttpStatusCode.OK ? 1 : 0);
            counts.UpdatedAddedSubsidiaries.Add(subsidiaryAddModel.Subsidiary);
        }

        counts.AlreadyExistCompanies = knownSubsidiariesToUpdateCheck.Select(org => org.Subsidiary).ToList();
        counts.UpdatedSubsidiariesRelationships = count;
        return counts;
    }
}