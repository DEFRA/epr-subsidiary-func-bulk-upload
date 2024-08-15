using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkUploadOrchestration : IBulkUploadOrchestration
{
    private readonly IRecordExtraction recordExtraction;
    private readonly ISubsidiaryService organisationService;
    private readonly IChildProcessor childProcessor;

    public BulkUploadOrchestration(IRecordExtraction recordExtraction, ISubsidiaryService organisationService, IChildProcessor childProcessor)
    {
        this.recordExtraction = recordExtraction;
        this.organisationService = organisationService;
        this.childProcessor = childProcessor;
    }

    public async Task Orchestrate(IEnumerable<CompaniesHouseCompany> data)
    {
        // this holds all the parents and their children records from cvs
        var parensAndhildrenRawCsv = recordExtraction.ExtractParentsAndChildren(data);

        // this will fectch data from the org database for all the parents
        var parentsandOrgs = parensAndhildrenRawCsv.Select(raw => RetrieveParentOrgData(raw));

        // this filters the parents will valid data in RPD
        parentsandOrgs = parentsandOrgs.Where(pao => pao.ParentOrgData != null);

        // this call will process the collection of subs(orgs) 
        parentsandOrgs.ToList().ForEach(m => childProcessor.Process(m.RawCsvData.Children, m.RawCsvData.Parent, m.ParentOrgData));
    }

    public (ParentWithChildren RawCsvData, OrganisationResponseModel ParentOrgData) RetrieveParentOrgData(ParentWithChildren source)
    {
        return (source, organisationService.GetCompanyByCompaniesHouseNumber(source.Parent.companies_house_number).Result);
    }
}
