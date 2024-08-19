using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkChildProcessor(ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService, ILogger<BulkChildProcessor> logger)
    : IChildProcessor
{
    private readonly ILogger<BulkChildProcessor> _logger = logger;
    private readonly ISubsidiaryService organisationService = organisationService;
    private readonly ICompaniesHouseLookupService companiesHouseLookupService = companiesHouseLookupService;

    public async Task Process(IEnumerable<CompaniesHouseCompany> children, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg, string userName)
    {
        foreach (var subsidiaryRecord in children)
        {
            // check if the subsidiary company exists in the database
            var subsidiaryResponse = await organisationService.GetCompanyByCompaniesHouseNumber(subsidiaryRecord.companies_house_number);

            // child company already exists in the database
            if (subsidiaryResponse != null)
            {
                try
                {
                    var isRelationshipExists = await organisationService.GetSubsidiaryRelationshipAysnc(parentOrg.id, subsidiaryResponse.id);
                    if (isRelationshipExists == true)
                    {
                        continue;
                    }

                    // Question for mike. why the org ids are defined as GUID and is this ever tested?
                    var existingSubsidiary = new SubsidiaryAddModel()
                    {
                        UserId = Guid.Parse(userName),
                        ParentOrganisationId = parentOrg.referenceNumber,
                        ChildOrganisationId = subsidiaryResponse.referenceNumber,
                        ParentOrganisationExternalId = parentOrg.ExternalId,
                        ChildOrganisationExternalId = subsidiaryResponse.ExternalId
                    };

                    // add new relationship for the child-parent
                    var localCreateResponse = await organisationService.AddSubsidiaryRelationshipAsync(existingSubsidiary);
                    _logger.LogInformation("Subsidiary Company {0} {1} linked to {2} in the database.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name, parent.organisation_name);
                    continue;
                }
                catch (Exception)
                {
                    // return Problem("Failed to create and add organisation", statusCode: StatusCodes.Status500InternalServerError);
                    _logger.LogError("Invalid Company Record Org Id : {OrganisationId} {Organisation_Name}. This cannot be created in Accounts Database", parent.organisation_id, parent.organisation_name);
                    continue;
                }
            }

            // company does not exist in org database. check if subsidiary company exists in the table storage (temp data)
            var tableStorageResponse = await organisationService.GetCompanyByOrgIdFromTableStorage(subsidiaryRecord.companies_house_number);
            if (tableStorageResponse != null)
            {
                try
                {
                   // company exists in temp storage (table storage)
                   var newSubsidiaryFromTS = new LinkOrganisationModel()
                    {
                        UserId = Guid.Parse(userName),
                        Subsidiary = new OrganisationModel()
                        {
                            ReferenceNumber = subsidiaryRecord.organisation_id,
                            Name = subsidiaryRecord.organisation_name,
                            CompaniesHouseNumber = subsidiaryRecord.companies_house_number,
                            OrganisationType = OrganisationType.NotSet,
                            ProducerType = ProducerType.Other,
                            Address = tableStorageResponse.Address,
                            IsComplianceScheme = false,
                            Nation = Nation.NotSet
                        },
                        ParentOrganisationId = parentOrg.ExternalId.Value
                    };
                   var tableStorageCreateResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiaryFromTS);
                   _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name);
                   _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name, parentOrg.name);
                   continue;
                }
                catch (Exception)
                {
                    _logger.LogError("Invalid Company Record in Table Storage : {OrganisationId} {Organisation_Name}. This cannot be created in Accounts Database", parent.organisation_id, parent.organisation_name);
                    continue;
                }
            }

            // Company does not exist in table storage. make call to comapanies house API
            var companyHouseResponse = await companiesHouseLookupService.GetCompaniesHouseResponseAsync(subsidiaryRecord.companies_house_number);
            if (companyHouseResponse != null)
            {
                try
                {
                    var newSubsidiaryFromCH = new LinkOrganisationModel()
                    {
                        UserId = Guid.Parse(userName),
                        Subsidiary = new OrganisationModel()
                        {
                            Name = companyHouseResponse.Name,
                            OrganisationType = OrganisationType.CompaniesHouseCompany,
                            ProducerType = ProducerType.Other,
                            IsComplianceScheme = false,
                            Nation = Nation.NotSet,
                            Address = new AddressModel()
                            {
                                BuildingNumber = companyHouseResponse.BusinessAddress.AddressSingleLine,
                                Street = companyHouseResponse.BusinessAddress.Street,
                                Country = companyHouseResponse.BusinessAddress.Country,
                                Locality = companyHouseResponse.BusinessAddress.Locality,
                                Postcode = companyHouseResponse.BusinessAddress.Postcode
                            },

                            ReferenceNumber = subsidiaryRecord.organisation_id,
                            CompaniesHouseNumber = subsidiaryRecord.companies_house_number,
                        },
                        ParentOrganisationId = parentOrg.ExternalId.Value
                    };

                    var createFromCompaniesHouseDaaResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiaryFromCH);
                    _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name);
                    _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name, parentOrg.name);
                    continue;
                }
                catch (Exception)
                {
                    _logger.LogError("Invalid Company Record in Companies House: {OrganisationId} {Organisation_Name}. This cannot be created in Accounts Database", parent.organisation_id, parent.organisation_name);
                    continue;
                }
            }
        }
    }
}
