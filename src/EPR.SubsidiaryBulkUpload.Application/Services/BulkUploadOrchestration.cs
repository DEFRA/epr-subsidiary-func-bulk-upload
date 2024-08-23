using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private const string SubsidiaryBulkUploadProgress = "Subsidiary bulk upload progress";

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

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel)
    {
        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadProgress);
        _notificationService.SetStatus(key, "Uploading");

        // this holds all the parents and their children records from csv
        var subsidiaryGroups = recordExtraction.ExtractParentsAndSubsidiaries(data).ToAsyncEnumerable();

        // this will fectch data from the org database for all the parents and filter to keep the valid ones (org exists in RPD)
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
}
