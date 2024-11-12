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

        var subsidiariesAndOrgWithValidName = subsidiariesAndOrg
            .Where(sub => sub.SubsidiaryOrg != null && sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber
            && string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg.name, StringComparison.OrdinalIgnoreCase));

        var subsidiariesAndOrgWithValidNameProcessStatistics = await ProcessValidNamedOrgs(subsidiariesAndOrgWithValidName, parentOrg, userRequestModel);

        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg.Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber
            && !string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg?.name, StringComparison.OrdinalIgnoreCase));
        var subWithInvalidName = await subsidiariesAndOrgWith_InValidName.Select(s => s.Subsidiary).ToListAsync();

        /*Scenario 2: The subsidiary found in RPD. name not match*/
        await ReportCompanies(subWithInvalidName, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInRPDMessage, BulkUpdateErrors.CompanyNameIsDifferentInRPD);

        var remainingToProcess = nonNullCompaniesHouseNumberRecords.Except(subWithInvalidName).Except(subsidiariesAndOrgWithValidNameProcessStatistics.NewAddedSubsidiaries);

        /*Scenario 3: The subsidiary found in Offline data. name matches then Add OR name not match then get it from CH API and name matches with CH API data.*/
        var newSubsidiariesToAdd_DataFromLocalStorageOrCH = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
        .SelectAwait(async subsidiary =>
            (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userRequestModel.UserId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        var companyHouseAPIProcessStatistics = await ProcessCompanyHouseAPI(newSubsidiariesToAdd_DataFromLocalStorageOrCH, userRequestModel);
        var remainingToProcessPart2 = remainingToProcess.Except(companyHouseAPIProcessStatistics.CompaniesHouseAPIErrorListReported);
        var remainingToProcessPart3 = remainingToProcessPart2.Except(companyHouseAPIProcessStatistics.NewAddedSubsidiaries).Except(companyHouseAPIProcessStatistics.DuplicateSubsidiaries);

        /*Scenario 4: The subsidiary found in Offline data. name not match. get it from CH API and name not matches with CH API data. Report Error.*/
        var newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouse_NameNoMatch = newSubsidiariesToAdd_DataFromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error == null
            && !string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.CompaniesHouseCompanyName, StringComparison.OrdinalIgnoreCase));

        var newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList = await newSubsidiariesToAdd_DataFromLocalStorageOrCompaniesHouse_NameNoMatch.Select(s => s.Subsidiary).ToListAsync();
        await ReportCompanies(newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPIMessage, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPI);

        var remainingToProcessPart4 = remainingToProcessPart3.Except(newSubsidiariesToAdd_DataFromLocalStorageOrCH_NameNoMatchList);

        var allAddedNewSubsPlusExisting = await newSubsidiariesToAdd_DataFromLocalStorageOrCH.Where(sta => sta.LinkModel.StatusCode == System.Net.HttpStatusCode.OK).Select(sta => sta.Subsidiary)
            .ToListAsync();

        /*Scenario 1: The subsidiary is not found in RPD and not in Local storage and not found on companies house*/
        await ReportCompanies(remainingToProcessPart4.Except(allAddedNewSubsPlusExisting), userRequestModel, BulkUpdateErrors.CompanyNameNotFoundAnywhereMessage, BulkUpdateErrors.CompanyNameNotFoundAnywhere);

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
                FileLineNumber = subsidiary.FileLineNumber
            },
            ParentOrganisationId = parentOrg.ExternalId.Value
        };

        var modelLoaded = await companiesHouseDataProvider.SetCompaniesHouseData(newSubsidiaryModel.Subsidiary);

        return modelLoaded ? newSubsidiaryModel : null;
    }

    private async Task<string> AddSubsidiary(OrganisationResponseModel parent, OrganisationResponseModel subsidiary, Guid userId)
    {
        var subsidiaryModel = new SubsidiaryAddModel
        {
            UserId = userId,
            ParentOrganisationId = parent.referenceNumber,
            ChildOrganisationId = subsidiary.referenceNumber,
            ParentOrganisationExternalId = parent.ExternalId,
            ChildOrganisationExternalId = subsidiary.ExternalId
        };
        var result = await organisationService.AddSubsidiaryRelationshipAsync(subsidiaryModel);

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);

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

        await foreach (var subsidiaryAddModel in knownFranchiseeToAddRelationship)
        {
            await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userRequestModel.UserId);
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

        return subsidiariesAndOrgNonExistsInTheDB.Where(s => s.Subsidiary.StatusCode == System.Net.HttpStatusCode.OK).Select(s => s.Subsidiary).ToList();
    }

    private async Task<AddSubsidiariesFigures> ProcessCompanyHouseAPI(IAsyncEnumerable<(CompaniesHouseCompany Subsidiary, LinkOrganisationModel LinkModel)> newSubsidiariesToAdd_DataFromLocalStorageOrCH, UserRequestModel userRequestModel)
    {
        /*Scenario : Companies house API Errors*/
        var counts = new AddSubsidiariesFigures();
        var result = string.Empty;

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

            if (result != null && subsidiaryAndLink.LinkModel.StatusCode == System.Net.HttpStatusCode.OK)
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
        var result = string.Empty;

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
            result = await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userRequestModel.UserId);
            count = count + (result != null ? 1 : 0);
            counts.NewAddedSubsidiaries.Add(subsidiaryAddModel.Subsidiary);
        }

        counts.NewAddedSubsidiariesRelationships = count;
        return counts;
    }
}