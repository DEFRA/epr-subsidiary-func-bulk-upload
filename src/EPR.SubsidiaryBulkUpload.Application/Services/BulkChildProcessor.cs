using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkChildProcessor : IChildProcessor
{
    private readonly ISubsidiaryService organisationService;
    private readonly ICompaniesHouseLookupService companiesHouseLookupService;

    public BulkChildProcessor(ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService)
    {
        this.organisationService = organisationService;
        this.companiesHouseLookupService = companiesHouseLookupService;
    }

    public async Task Process(IEnumerable<CompaniesHouseCompany> children, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg)
    {
        foreach (var subsidiaryRecord in children)
        {
            OrganisationModel subsidiary = new OrganisationModel()
            {
                ReferenceNumber = subsidiaryRecord.organisation_id,
                Name = subsidiaryRecord.organisation_name,
                CompaniesHouseNumber = subsidiaryRecord.companies_house_number
            };

            // check if the subsidiary company exists in the database
            var subsidiaryResponse = await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.CompaniesHouseNumber);

            // child company already exists in the database
            if (subsidiaryResponse != null)
            {
                var isRelationshipExists = await organisationService.GetSubsidiaryRelationshipAysnc(parentOrg.id, subsidiaryResponse.id);
                if (isRelationshipExists == true)
                {
                    continue;
                }

                // Question for mike. why the org ids are defined as GUID and is this ever tested?
                SubsidiaryAddModel existingSubsidiary = new SubsidiaryAddModel()
                {
                    UserId = Guid.Parse(_user),
                    ParentOrganisationId = parentOrg.referenceNumber,
                    ChildOrganisationId = subsidiaryResponse.referenceNumber,
                    ParentOrganisationExternalId = parentOrg.ExternalId,
                    ChildOrganisationExternalId = subsidiaryResponse.ExternalId
                };

                // add new relationship for the child-parent
                var localCreateResponse = await organisationService.AddSubsidiaryRelationshipAsync(existingSubsidiary);
                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name, record.organisation_name);
                continue;
            }
            else
            {
                // return Problem("Failed to create and add organisation", statusCode: StatusCodes.Status500InternalServerError);
                _logger.LogError("Invalid Company Record Org Id : {OrganisationId} {Organisation_Name}. This cannot be created in Accounts Database", record.organisation_id, record.organisation_name);
                continue; // throw new Exception("Invalid Subsidiary Record. ");
            }
        }
    }
}
