using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private readonly IRecordExtraction recordExtraction;
    private readonly ISubsidiaryService organisationService;
    private readonly IBulkSubsidiaryProcessor childProcessor;

    public BulkUploadOrchestration(IRecordExtraction recordExtraction, ISubsidiaryService organisationService, IBulkSubsidiaryProcessor childProcessor)
    {
        this.recordExtraction = recordExtraction;
        this.organisationService = organisationService;
        this.childProcessor = childProcessor;
    }

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, Guid userId)
    {
        // this holds all the parents and their children records from csv
        var subsidiaryGroups = recordExtraction.ExtractParentsAndSubsidiaries(data).ToAsyncEnumerable();

        // this will fectch data from the org database for all the parents and filter to keep the valid ones (org exists in RPD)
        var subsidiaryGroupsAndParentOrg = subsidiaryGroups.SelectAwait(
            async sg => (SubsidiaryGroup: sg, Org: await organisationService.GetCompanyByCompaniesHouseNumber(sg.Parent.companies_house_number)))
            .Where(sg => sg.Org != null);

        await foreach(var subsidiaryGroupAndParentOrg in subsidiaryGroupsAndParentOrg)
        {
            await childProcessor.Process(
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Subsidiaries,
                subsidiaryGroupAndParentOrg.SubsidiaryGroup.Parent,
                subsidiaryGroupAndParentOrg.Org,
                userId);
        }
    }
}
