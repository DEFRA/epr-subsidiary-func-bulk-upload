using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private const string SubsidiaryBulkUploadProgress = "Subsidiary bulk upload progress";
    private const string SubsidiaryBulkUploadErrors = "Subsidiary bulk upload errors";

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

    public async Task NotifyErrors(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress);
        _notificationService.SetStatus(key, "Started Data Validation");
        var notificationErrorList = new List<UploadFileErrorModel>();
        var dataWithErrors = data.Where(e => e.UploadFileErrorModel != null).ToList();
        foreach (var company in dataWithErrors)
        {
            if (company.Errors.Length > 0)
            {
                notificationErrorList.Add(company.UploadFileErrorModel);
            }
        }

        var keyErrors = userRequestModel.GenerateKey(SubsidiaryBulkUploadErrors);
        _notificationService.SetStatus(key, "Error found in validation. Logging it in Redis storage");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _notificationService.SetStatus(key, "Finished Data Validation");
    }

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress);
        _notificationService.SetStatus(key, "Uploading");

        // this holds all the parents and their children records from csv
        var subsidiaryGroups = recordExtraction.ExtractParentsAndSubsidiaries(data).ToAsyncEnumerable();

        // this will fetch data from the org database for all the parents and filter to keep the valid ones (org exists in RPD)
        var subsidiaryGroupsAndParentOrg = subsidiaryGroups.SelectAwait(
            async sg => (SubsidiaryGroup: sg, Org: await organisationService.GetCompanyByCompaniesHouseNumber(sg.Parent.companies_house_number)))
            .Where(sg => sg.Org != null);

        await foreach (var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrg)
        {
            await childProcessor.Process(
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.Org,
                userRequestModel.UserId);
        }

        _notificationService.SetStatus($"{userRequestModel.UserId}{userRequestModel.OrganisationId}{SubsidiaryBulkUploadProgress}", "Finished");
    }

    private async Task NotifyErrorsProcessing(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel, string keyValue)
    {
        var key = userRequestModel.GenerateKey(keyValue);
        _notificationService.SetStatus(Guid.NewGuid().ToString(), "Started NotifyErrorsProcessing");
        var notificationErrorList = new List<UploadFileErrorModel>();
        var dataWithErrors = data.Where(e => e.UploadFileErrorModel != null).ToList();
        foreach (var company in dataWithErrors)
        {
            if (company.Errors.Length > 0)
            {
                notificationErrorList.Add(company.UploadFileErrorModel);
            }
        }

        var keyErrors = userRequestModel.GenerateKey(SubsidiaryBulkUploadErrors);
        _notificationService.SetStatus(key, "Error found in validation. Logging it in Redis storage");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _notificationService.SetStatus(Guid.NewGuid().ToString(), "Finished NotifyErrorsProcessing");
    }
}
