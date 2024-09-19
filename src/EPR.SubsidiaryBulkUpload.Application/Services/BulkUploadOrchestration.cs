using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private const string SubsidiaryBulkUploadInvalidDataErrors = "Subsidiary bulk upload File errors";
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
        if (data.Count() == 0)
        {
            var fileValidation = new List<UploadFileErrorModel>();
            var newError = new UploadFileErrorModel()
            {
                FileContent = "No Record found in the file.",
                Message = "No Record found in the file"
            };
            fileValidation.Add(newError);
            _notificationService.SetStatus(userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress), "Error found in validation. Logging it in Redis storage");
            _notificationService.SetErrorStatus(userRequestModel.GenerateKey(SubsidiaryBulkUploadInvalidDataErrors), fileValidation);
            return;
        }

        var notificationErrorList = data
            .Where(e => e.UploadFileErrorModel != null)
            .Select(e => e.UploadFileErrorModel)
            .ToList();

        if(notificationErrorList.Count == 0)
        {
            return;
        }

        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(SubsidiaryBulkUploadErrors);

        _notificationService.SetStatus(key, "Error found in validation. Logging it in Redis storage");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
    }

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress);
        _notificationService.SetStatus(key, "Uploading");

        // this holds all the parents and their children records from csv
        var subsidiaryGroups = recordExtraction
            .ExtractParentsAndSubsidiaries(data.Where(r => r.UploadFileErrorModel is null))
            .ToAsyncEnumerable();

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
                userRequestModel);
        }

        _notificationService.SetStatus(key, "Finished");
    }
}
