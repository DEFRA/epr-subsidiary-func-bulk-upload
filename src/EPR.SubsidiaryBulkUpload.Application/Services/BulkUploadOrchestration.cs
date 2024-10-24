﻿using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private readonly IRecordExtraction recordExtraction;
    private readonly ISubsidiaryService organisationService;
    private readonly IBulkSubsidiaryProcessor childProcessor;
    private readonly INotificationService _notificationService;

    public BulkUploadOrchestration(IRecordExtraction recordExtraction, ISubsidiaryService organisationService, IBulkSubsidiaryProcessor childProcessor, INotificationService notificationService)
    {
        this.recordExtraction = recordExtraction;
        this.organisationService = organisationService;
        this.childProcessor = childProcessor;
        _notificationService = notificationService;
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
                Message = "File has no records.",
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
        var subsidiaryGroupsAndParentOrg = subsidiaryGroups.SelectAwait(
            async sg => (SubsidiaryGroup: sg, Org: await organisationService.GetCompanyByCompaniesHouseNumber(sg.Parent.companies_house_number)))
            .Where(sg => sg.Org != null);

        var addedSubsidiariesCount = 0;

        await foreach (var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrg)
        {
            addedSubsidiariesCount += await childProcessor.Process(
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.Org,
                userRequestModel);
        }

        _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress), "Finished");
        _notificationService.SetStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded), addedSubsidiariesCount.ToString());
    }
}
