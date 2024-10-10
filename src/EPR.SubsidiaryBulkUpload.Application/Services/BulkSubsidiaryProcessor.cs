using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
        var addedSubsidiariesCount = 0;

        var subsidiariesAndOrg = subsidiaries
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.companies_house_number)));

        var subsidiariesAndOrgWithValidName = subsidiariesAndOrg
            .Where(sub => sub.SubsidiaryOrg != null && sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber
            && string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg.name, StringComparison.OrdinalIgnoreCase));

        var knownSubsidiariesToAdd = subsidiariesAndOrgWithValidName.Where(co => co.SubsidiaryOrg != null)
            .SelectAwait(async co =>
                (Subsidiary: co.Subsidiary,
                 SubsidiaryOrg: co.SubsidiaryOrg,
                 RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id)))
            .Where(co => !co.RelationshipExists);

        await foreach (var subsidiaryAddModel in knownSubsidiariesToAdd)
        {
            await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userRequestModel.UserId, subsidiaryAddModel.Subsidiary);
            addedSubsidiariesCount++;
        }

        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg
            .Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber
            && !string.Equals(sub.Subsidiary.organisation_name, sub.SubsidiaryOrg?.name, StringComparison.OrdinalIgnoreCase));
        var subWithInvalidName = await subsidiariesAndOrgWith_InValidName.Select(s => s.Subsidiary).ToListAsync();

        /*Scenario 2: The subsidiary found in RPD. name not match*/
        await ReportCompanies(subWithInvalidName, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInRPDMessage, BulkUpdateErrors.CompanyNameIsDifferentInRPD);

        /*Scenario 3: The subsidiary found in Offline data. name matches then Add OR name not match then get it from CH API and name matches with CH API data.*/
        var newSubsidiariesToAdd_DatafromLocalStorageOrCH = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
        .SelectAwait(async subsidiary =>
            (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userRequestModel.UserId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        /*Scenario : Companies house API Errors*/
        var companiesHouseAPIErrorList = await newSubsidiariesToAdd_DatafromLocalStorageOrCH.Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error != null).Select(s => s.LinkModel).ToListAsync();
        await ReportCompanies(companiesHouseAPIErrorList, userRequestModel);

        var newSubsidiariesToAdd_DatafromLocalStorageOrCompaniesHouseWithNameMatch = newSubsidiariesToAdd_DatafromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error == null &&
            (string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.LocalStorageName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.CompaniesHouseCompanyName, StringComparison.OrdinalIgnoreCase)));

        await foreach (var subsidiaryandLink in newSubsidiariesToAdd_DatafromLocalStorageOrCompaniesHouseWithNameMatch)
        {
            subsidiaryandLink.LinkModel.StatusCode = await organisationService.CreateAndAddSubsidiaryAsync(subsidiaryandLink.LinkModel);
            addedSubsidiariesCount++;
        }

        /*Scenario 4: The subsidiary found in Offline data. name not match. get it from CH API and name not matches with CH API data. Report Error.*/
        var newSubsidiariesToAdd_DatafromLocalStorageOrCompaniesHouse_NameNoMatch = newSubsidiariesToAdd_DatafromLocalStorageOrCH
            .Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error == null
            && !string.Equals(subAndLink.Subsidiary.organisation_name, subAndLink.LinkModel.Subsidiary.CompaniesHouseCompanyName, StringComparison.OrdinalIgnoreCase));

        var newSubsidiariesToAdd_DatafromLocalStorageOrCH_NameNoMatchList = await newSubsidiariesToAdd_DatafromLocalStorageOrCompaniesHouse_NameNoMatch.Select(s => s.Subsidiary).ToListAsync();

        await ReportCompanies(newSubsidiariesToAdd_DatafromLocalStorageOrCH_NameNoMatchList, userRequestModel, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPIMessage, BulkUpdateErrors.CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPI);

        var allAddedNewSubsPlusExisting = await newSubsidiariesToAdd_DatafromLocalStorageOrCH.Where(sta => sta.LinkModel.StatusCode == System.Net.HttpStatusCode.OK).Select(sta => sta.Subsidiary)
            .Concat(subsidiariesAndOrgWithValidName.Select(swoan => swoan.Subsidiary))
            .ToListAsync();

        var subsidiariesNotAdded = subsidiaries.AsEnumerable().Except(allAddedNewSubsPlusExisting).Except(subWithInvalidName).Except(newSubsidiariesToAdd_DatafromLocalStorageOrCH_NameNoMatchList).Except(await newSubsidiariesToAdd_DatafromLocalStorageOrCH.Where(subAndLink => subAndLink.LinkModel != null && subAndLink.LinkModel.Subsidiary.Error != null).Select(s => s.Subsidiary).ToListAsync());

        /*Scenario 1: The subsidiary is not found in RPD and not in Local storage and not found on companies house*/
        await ReportCompanies(subsidiariesNotAdded, userRequestModel, BulkUpdateErrors.CompanyNameNofoundAnywhereMessage, BulkUpdateErrors.CompanyNameNofoundAnywhere);

        return addedSubsidiariesCount;
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
        _notificationService.SetStatus(key, "Error Reporting.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
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
        _notificationService.SetStatus(key, "Started reporting invalid subsidiaries.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation(errorMessage);
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
                OrganisationType = OrganisationType.NotSet,
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

    private async Task AddSubsidiary(OrganisationResponseModel parent, OrganisationResponseModel subsidiary, Guid userId, CompaniesHouseCompany subsidiaryFileData)
    {
        var subsidiaryModel = new SubsidiaryAddModel
        {
            UserId = userId,
            ParentOrganisationId = parent.referenceNumber,
            ChildOrganisationId = subsidiary.referenceNumber,
            ParentOrganisationExternalId = parent.ExternalId,
            ChildOrganisationExternalId = subsidiary.ExternalId
        };
        await organisationService.AddSubsidiaryRelationshipAsync(subsidiaryModel);

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);
    }
}