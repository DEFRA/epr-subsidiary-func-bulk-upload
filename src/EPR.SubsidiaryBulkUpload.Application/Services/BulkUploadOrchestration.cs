using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private readonly IRecordExtraction _recordExtraction;
    private readonly ISubsidiaryService _organisationService;
    private readonly IBulkSubsidiaryProcessor _childProcessor;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BulkUploadOrchestration> _logger;
    private readonly string orphanRecord = "orphan";

    public BulkUploadOrchestration(IRecordExtraction recordExtraction, ISubsidiaryService organisationService, IBulkSubsidiaryProcessor childProcessor, INotificationService notificationService, ILogger<BulkUploadOrchestration> logger)
    {
        _recordExtraction = recordExtraction;
        _organisationService = organisationService;
        _childProcessor = childProcessor;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task NotifyStart(UserRequestModel userRequestModel)
    {
        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);
        await _notificationService.SetStatus(key, "Uploading");
        await _notificationService.ClearRedisKeyAsync(keyErrors);
    }

    public async Task NotifyErrors(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        if (!data.Any())
        {
            var fileValidation = new List<UploadFileErrorModel>();
            var newError = new UploadFileErrorModel()
            {
                FileLineNumber = 2,
                FileContent = string.Empty,
                Message = BulkUpdateErrors.FileHasNoRecord,
                ErrorNumber = BulkUpdateErrors.FileEmptyError,
                IsError = true
            };

            fileValidation.Add(newError);
            await _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Error");
            await _notificationService.SetErrorStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors), fileValidation);
            return;
        }

        var errors = data.Where(d => d.Errors != null).SelectMany(chc => chc.Errors).ToList();

        if (errors.Count != 0)
        {
            var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
            var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);

            await _notificationService.SetStatus(key, "Error");
            await _notificationService.SetErrorStatus(keyErrors, errors);
        }
    }

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        // this holds all the parents and their children records from csv
        var dataRefined = await FilterDuplicateParentSubsidiaries(userRequestModel, data.Where(r => !r.Errors.Any()));

        var subsidiaryGroups = _recordExtraction
            .ExtractParentsAndSubsidiaries(dataRefined.Where(r => !r.Errors.Any()))
            .ToAsyncEnumerable();

        var subsidiaryGroupsWithoutParentOrg = await subsidiaryGroups.Where(p => p.Parent.organisation_name == "orphan").ToListAsync();
        await ReportCompanies(subsidiaryGroupsWithoutParentOrg.ToList(), userRequestModel, BulkUpdateErrors.OrphanRecordParentOrganisationIsNotFoundErrorMessage, BulkUpdateErrors.OrphanRecordParentOrganisationIsNotFound);

        var subsidiaryGroupsWithValidParents = subsidiaryGroups.Where(p => p.Parent.organisation_name != "orphan");

        // this will fetch data from the org database for all the parents and filter to keep the valid ones (org exists in RPD)
        var subsidiaryGroupsAndParentOrg = await subsidiaryGroupsWithValidParents.SelectAwait(
            async sg => (SubsidiaryGroup: sg, parentOrg: await _organisationService.GetCompanyByReferenceNumber(sg.Parent.organisation_id))).ToListAsync();

        // parents not found report
        var subsidiaryGroupsAndParentOrgWithParentNotFound = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg == null).Select(s => s.SubsidiaryGroup);
        await ReportCompanies(subsidiaryGroupsAndParentOrgWithParentNotFound.ToList(), userRequestModel, BulkUpdateErrors.ParentOrganisationIsNotFoundErrorMessage, BulkUpdateErrors.ParentOrganisationIsNotFound);

        // parents companies house company number not found report
        var subsidiaryGroupsAndParentOrgWith_InvalidCompaniesHouseNumber = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg != null && sg.SubsidiaryGroup.Parent.companies_house_number != sg.parentOrg.companiesHouseNumber).Select(s => s.SubsidiaryGroup);
        await ReportCompanies(subsidiaryGroupsAndParentOrgWith_InvalidCompaniesHouseNumber.ToList(), userRequestModel, BulkUpdateErrors.ParentOrganisationFoundCompaniesHouseNumberNotMatchingMessage, BulkUpdateErrors.ParentOrganisationFoundCompaniesHouseNumberNotMatching);

        var subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg != null && sg.SubsidiaryGroup.Parent.companies_house_number == sg.parentOrg.companiesHouseNumber);

        // Scenario 1: Parent with valid ID but no child
        var parentWithNoChild = subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber.Where(p => p.SubsidiaryGroup.Subsidiaries.Count == 0).Select(s => s.SubsidiaryGroup.Parent).ToList();
        await ReportCompanies(parentWithNoChild, userRequestModel, BulkUpdateErrors.ParentOrganisationWithNoChildErrorMessage, BulkUpdateErrors.ParentOrganisationWithNoChildError);

        var addedSubsidiariesCount = 0;

        foreach (var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber.Where(o => o.SubsidiaryGroup.Subsidiaries.Count > 0).ToList())
        {
            var subsidiariesToProcess = await FilterDuplicateSubsidiaries(userRequestModel, subsidiaryGroupAndParentOrg);

            addedSubsidiariesCount += await _childProcessor.Process(
                subsidiariesToProcess,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.parentOrg,
                userRequestModel);
        }

        await _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Finished");
        await _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded), addedSubsidiariesCount.ToString());
    }

    private async Task<List<CompaniesHouseCompany>> FilterDuplicateSubsidiaries(
        UserRequestModel userRequestModel,
        (ParentAndSubsidiaries SubsidiaryGroup, OrganisationResponseModel? ParentOrg) subsidiaryGroupAndParentOrg)
    {
        var duplicatesGroupList = subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries.GroupBy(companiesHouseCompany => new
        {
            companiesHouseCompany.organisation_id,
            companiesHouseCompany.organisation_name,
            companiesHouseCompany.companies_house_number,
            companiesHouseCompany.parent_child
        }).Where(grouping => grouping.Key.parent_child.Equals("child", StringComparison.InvariantCultureIgnoreCase)).ToList();

        var subsidiariesToProcess = subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries;

        foreach (var group in duplicatesGroupList)
        {
            if (group.Count() > 1)
            {
                var duplicateItems = subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries.Where(company =>

                    company.organisation_id == group.Key.organisation_id &&
                    company.organisation_name == group.Key.organisation_name &&
                    company.companies_house_number == group.Key.companies_house_number &&
                    company.parent_child == group.Key.parent_child).ToList();

                duplicateItems.RemoveAt(0);

                await ReportCompanies(duplicateItems, userRequestModel, BulkUpdateErrors.DuplicateRecordsErrorMessage, BulkUpdateErrors.DuplicateRecordsError);
                subsidiariesToProcess = subsidiariesToProcess.Except(duplicateItems).ToList();
            }
        }

        return subsidiariesToProcess;
    }

    private async Task<IEnumerable<CompaniesHouseCompany>> FilterDuplicateParentSubsidiaries(
        UserRequestModel userRequestModel, IEnumerable<CompaniesHouseCompany> source)
    {
        var duplicatesGroupList = source.GroupBy(companiesHouseCompany => new
        {
            companiesHouseCompany.organisation_id,
            companiesHouseCompany.parent_child
        }).Where(grouping => grouping.Key.parent_child.Equals("parent", StringComparison.InvariantCultureIgnoreCase)).ToList();

        var subsidiariesToProcess = source;

        foreach (var group in duplicatesGroupList)
        {
            if (group.Count() > 1)
            {
                var duplicateItems = source.Where(company =>

                    company.organisation_id == group.Key.organisation_id &&
                    company.parent_child == group.Key.parent_child).ToList();

                duplicateItems.RemoveAt(0);

                await ReportCompanies(duplicateItems, userRequestModel, BulkUpdateErrors.DuplicateRecordsErrorMessage, BulkUpdateErrors.DuplicateRecordsError);
                subsidiariesToProcess = subsidiariesToProcess.Except(duplicateItems);
            }
        }

        return subsidiariesToProcess;
    }

    private async Task ReportCompanies(List<ParentAndSubsidiaries> subsidiaryGroupsAndParentOrgWithParentNotFound, UserRequestModel userRequestModel, string errorMessage, int errorNumber)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in subsidiaryGroupsAndParentOrgWithParentNotFound)
        {
            company.Parent.Errors = new List<UploadFileErrorModel>();
            var newError = new UploadFileErrorModel()
            {
                FileLineNumber = company.Parent.FileLineNumber,
                FileContent = company.Parent.RawRow,
                Message = errorMessage,
                IsError = true,
                ErrorNumber = errorNumber
            };

            if (company.Parent.organisation_name != orphanRecord)
            {
                notificationErrorList.Add(newError);
            }

            company.Parent.Errors = notificationErrorList;

            if (company.Subsidiaries.Count > 0)
            {
                await ReportCompanies(company.Subsidiaries, userRequestModel, errorMessage, errorNumber);
            }
        }

        if (notificationErrorList.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);
        await _notificationService.SetStatus(key, "Error Reporting Parent Record.");
        await _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation("{ErrorMessage}", errorMessage);
    }

    private async Task ReportCompanies(List<CompaniesHouseCompany> subsidiaries, UserRequestModel userRequestModel, string errorMessage, int errorNumber)
    {
        var notificationErrorListForSubsidiaries = new List<UploadFileErrorModel>();
        foreach (var subsidiary in subsidiaries)
        {
            var newSubError = new UploadFileErrorModel()
            {
                FileLineNumber = subsidiary.FileLineNumber,
                FileContent = subsidiary.RawRow,
                Message = errorMessage,
                IsError = true,
                ErrorNumber = errorNumber
            };
            notificationErrorListForSubsidiaries.Add(newSubError);
        }

        if (notificationErrorListForSubsidiaries.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);
        await _notificationService.SetStatus(key, "Error Reporting Child Record.");
        await _notificationService.SetErrorStatus(keyErrors, notificationErrorListForSubsidiaries);
        _logger.LogInformation("{ErrorMessage}", errorMessage);
    }
}