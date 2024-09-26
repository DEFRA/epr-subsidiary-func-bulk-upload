using EPR.SubsidiaryBulkUpload.Application.DTOs;
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
        _notificationService.SetStatus(key, "Uploading");
        _notificationService.SetErrorStatus(userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors), new List<UploadFileErrorModel>());
    }

    public async Task NotifyErrors(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        if (data.Count() == 0)
        {
            var fileValidation = new List<UploadFileErrorModel>();
            var newError = new UploadFileErrorModel()
            {
                FileContent = "No Record found in the file.",
                Message = "No Record found in the file",
                ErrorNumber = BulkUpdateErrors.FileEmptyError
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
        var key = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);

        // this holds all the parents and their children records from csv
        var subsidiaryGroups = recordExtraction
            .ExtractParentsAndSubsidiaries(data.Where(r => !r.Errors.Any()))
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
