using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private readonly IRecordExtraction recordExtraction;
    private readonly ISubsidiaryService organisationService;
    private readonly IBulkSubsidiaryProcessor childProcessor;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BulkUploadOrchestration> _logger;

    public BulkUploadOrchestration(IRecordExtraction recordExtraction, ISubsidiaryService organisationService, IBulkSubsidiaryProcessor childProcessor, INotificationService notificationService, ILogger<BulkUploadOrchestration> logger)
    {
        this.recordExtraction = recordExtraction;
        this.organisationService = organisationService;
        this.childProcessor = childProcessor;
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
        var subsidiaryGroups = recordExtraction
            .ExtractParentsAndSubsidiaries(data.Where(r => !r.Errors.Any()))
            .ToAsyncEnumerable();

        // this will fetch data from the org database for all the parents and filter to keep the valid ones (org exists in RPD)
        var subsidiaryGroupsAndParentOrg = await subsidiaryGroups.SelectAwait(
            async sg => (SubsidiaryGroup: sg, parentOrg: await organisationService.GetCompanyByReferenceNumber(sg.Parent.organisation_id))).ToListAsync();

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

        foreach (var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber)
        {
            addedSubsidiariesCount += await childProcessor.Process(
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.parentOrg,
                userRequestModel);
        }

        await _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Finished");
        await _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded), addedSubsidiariesCount.ToString());
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
            notificationErrorList.Add(newError);
            company.Parent.Errors = notificationErrorList;

            if (company.Subsidiaries.Count > 0)
            {
                ReportCompanies(company.Subsidiaries, userRequestModel, BulkUpdateErrors.ParentOrganisationNotValidChildCannotBeProcessedErrorMessage, BulkUpdateErrors.ParentOrganisationNotValidChildCannotBeProcessed);
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
