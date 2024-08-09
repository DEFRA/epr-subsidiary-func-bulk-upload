using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(
        ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;
        private readonly string _user = "E138C7A1-49B2-402B-B9B4-AD60A2282530";

        public async Task<int> ProcessStream(Stream stream, ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService)
        {
            var rowCount = 0;
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            // using var blobStreamReader = new StreamReader(stream);
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Context.RegisterClassMap<CompaniesHouseCompanyMap>();
                var records = csv.GetRecords<CompaniesHouseCompany>().ToList();

                // question. in one CSV will organisationId (1st column) will already be same? i.e parentID
                var parentRecords = records.Where(c => c.parent_child == "Parent").ToList();

                foreach (var record in parentRecords)
                {
                    // check if the parent company exists in the database
                    if (!string.IsNullOrEmpty(record.companies_house_number))
                    {
                        var response = await organisationService.GetCompanyByCompaniesHouseNumber(record.companies_house_number);

                        if (response == null)
                        {
                            // return Problem("Failed to create and add organisation", statusCode: StatusCodes.Status500InternalServerError);
                            _logger.LogError("Invalid Parent Record Org Id : {OrganisationId} {Organisation_Name}", record.organisation_id, record.organisation_name);
                            continue;
                        }

                        OrganisationResponseModel parentOrg = response;
                        var childRecords = records.Where(c => c.parent_child == "Child" && c.organisation_id == parentOrg.referenceNumber && string.IsNullOrEmpty(c.franchisee_licensee_tenant)).ToList();

                        // question. in one CSV will organisationId (1st column) will already be same? i.e parentID
                        // var childRecords = records.Where(c => c.organisation_id.Equals(record.organisation_id)).ToList();
                        foreach (var subsidiaryRecord in childRecords)
                        {
                            OrganisationModel subsidiary = new OrganisationModel()
                            {
                                ReferenceNumber = subsidiaryRecord.organisation_id,
                                Name = subsidiaryRecord.organisation_name,
                                CompaniesHouseNumber = subsidiaryRecord.companies_house_number
                            };

                            // check if the subsidiary company exists in the database
                            var subsidiaryResponse = await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.CompaniesHouseNumber);

                            var isRelationshipExists = await organisationService.GetSubsidiaryRelationshipAysnc(parentOrg.companiesHouseNumber, subsidiary.CompaniesHouseNumber);

                            // child company already exists in the database
                            if (subsidiaryResponse != null && isRelationshipExists == false)
                            {
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

                            // company does not exist in org database. check if subsidiary company exists in the table storage (temp data)
                            var tableStorageResponse = await organisationService.GetCompanyByOrgIdFromTableStorage(subsidiary.CompaniesHouseNumber);
                            if (tableStorageResponse != null)
                            {
                                subsidiary.OrganisationType = OrganisationType.NotSet;
                                subsidiary.ProducerType = ProducerType.Other;
                                subsidiary.Address = tableStorageResponse.Address;
                                subsidiary.IsComplianceScheme = false;
                                subsidiary.Nation = Nation.NotSet;

                                // company exists in temp storage (table storage)
                                LinkOrganisationModel newSubsidiaryFromTS = new LinkOrganisationModel()
                                {
                                    UserId = Guid.Parse(_user),
                                    Subsidiary = subsidiary,
                                    ParentOrganisationId = parentOrg.ExternalId.Value
                                };
                                var tableStorageCreateResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiaryFromTS);
                                _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name);
                                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name, record.organisation_name);
                                continue;
                            }

                            // Company does not exist in table storage. make call to comapanies house API
                            var companyHouseResponse = await companiesHouseLookupService.GetCompaniesHouseResponseAsync(subsidiary.CompaniesHouseNumber);
                            if (companyHouseResponse != null)
                            {
                                // company exists in companies hosue database
                                LinkOrganisationModel newSubsidiaryFromCH = new LinkOrganisationModel()
                                {
                                    UserId = Guid.Parse(_user),
                                    Subsidiary = subsidiary
                                };

                                var createFromCompaniesHouseDaaResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiaryFromCH);
                                _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", subsidiaryRecord.organisation_id, subsidiaryRecord.organisation_name);
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

                        // this for none companies house companies or franchisee etc. where to store these? and what information to store and where the information will come from
                        foreach (var franchiseeRecord in childRecords.Where(c => c.parent_child.ToUpper() == "CHILD" && !string.IsNullOrEmpty(c.franchisee_licensee_tenant) && c.franchisee_licensee_tenant.ToUpper() == "Y").ToList())
                        {
                            OrganisationModel franchisee = new OrganisationModel()
                            {
                                ReferenceNumber = record.organisation_id,
                                Name = record.organisation_name,
                                CompaniesHouseNumber = record.companies_house_number
                            };

                            LinkOrganisationModel franchiseeToCreate = new LinkOrganisationModel()
                            {
                                UserId = Guid.Parse(_user),
                                Subsidiary = franchisee
                            };

                            var franchiseeCreateResponse = await organisationService.CreateAndAddSubsidiaryAsync(franchiseeToCreate);

                            if (franchiseeCreateResponse == null)
                            {
                                // return Problem("Failed to create and add franchisee", statusCode: StatusCodes.Status500InternalServerError);
                                _logger.LogInformation("Franchisee Company added to the database : {OrganisationId} {Organisation_Name}.", franchisee.ReferenceNumber, franchisee.Name);
                                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", franchisee.ReferenceNumber, franchisee.Name, record.organisation_name);
                            }
                        }
                    }
                }
            }

            return rowCount;
        }

        public Task<int> ProcessStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
