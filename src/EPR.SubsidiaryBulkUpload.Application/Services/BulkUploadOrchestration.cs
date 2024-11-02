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
        _notificationService.SetStatus(key, "Uploading");
        _notificationService.ClearRedisKeyAsync(keyErrors);
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
            _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Error");
            _notificationService.SetErrorStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors), fileValidation);
            return;
        }

        var errors = data.Where(d => d.Errors != null).SelectMany(chc => chc.Errors).ToList();

        if (errors.Any())
        {
            var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
            var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);

            _notificationService.SetStatus(key, "Error");
            _notificationService.SetErrorStatus(keyErrors, errors);
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
            async sg => (SubsidiaryGroup: sg, parentOrg: await organisationService.GetCompanyByRefernceNumber(sg.Parent.organisation_id))).ToListAsync();

        // parents not found report
        var subsidiaryGroupsAndParentOrgWithParentNotfound = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg == null).Select(s => s.SubsidiaryGroup);
        var refNumberNotFound = subsidiaryGroupsAndParentOrgWithParentNotfound.ToList();
        await ReportCompanies(subsidiaryGroupsAndParentOrgWithParentNotfound.ToList(), userRequestModel, BulkUpdateErrors.ParentOrganisationIsNotFoundErrorMessage, BulkUpdateErrors.ParentOrganisationIsNotFound);

        // parents companies hosue company number not found report
        var subsidiaryGroupsAndParentOrgWith_InvalidCompaniesHouseNumber = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg != null && sg.SubsidiaryGroup.Parent.companies_house_number != sg.parentOrg.companiesHouseNumber).Select(s => s.SubsidiaryGroup);
        await ReportCompanies(subsidiaryGroupsAndParentOrgWith_InvalidCompaniesHouseNumber.ToList(), userRequestModel, BulkUpdateErrors.ParentOrganisationFoundCompaniesHouseNumberNotMatchingMessage, BulkUpdateErrors.ParentOrganisationFoundCompaniesHouseNumberNotMatching);

        var subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber = subsidiaryGroupsAndParentOrg.Where(sg => sg.parentOrg != null && sg.SubsidiaryGroup.Parent.companies_house_number == sg.parentOrg.companiesHouseNumber);
        var count1 = subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber.Select(o => o.parentOrg).Count();

        var addedSubsidiariesCount = 0;

        foreach (var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrgWithValidCompaniesHouseNumber)
        {
            addedSubsidiariesCount += await childProcessor.Process(
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.parentOrg,
                userRequestModel);
        }

        _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Finished");
        _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded), addedSubsidiariesCount.ToString());
    }

    private async Task ReportCompanies(List<ParentAndSubsidiaries> subsidiaryGroupsAndParentOrgWithParentNotfound, UserRequestModel userRequestModel, string errorMessage, int errorNumber)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in subsidiaryGroupsAndParentOrgWithParentNotfound)
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
                ReportCompanies(company.Subsidiaries, userRequestModel, BulkUpdateErrors.ParentOrganisationNotValidChildCannotbeProcessedErrorMessage, BulkUpdateErrors.ParentOrganisationNotValidChildCannotbeProcessed);
            }
        }

        if (notificationErrorList.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgressParent + Guid.NewGuid());
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrorsParent + Guid.NewGuid());
        _notificationService.SetStatus(key, "Error Reporting Parent Record.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation(errorMessage);
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

        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress + Guid.NewGuid());
        var keyErrors = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors + Guid.NewGuid());
        _notificationService.SetStatus(key, "Error Reporting Child Record.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorListForSubsidiaries);
        _logger.LogInformation(errorMessage);
    }
}
