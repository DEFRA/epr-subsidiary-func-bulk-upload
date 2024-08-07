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
                var records = csv.GetRecords<CompaniesHouseCompany>();

                // question. in one CSV will organisationId (1st column) will already be same? i.e parentID
                var parentRecords = records.Where(c => c.parent_child == "Parent").ToList();

                foreach (var record in parentRecords)
                {
                    // check if the parent company exists in the database
                    if (!string.IsNullOrEmpty(record.companies_house_number))
                    {
                        var response = await organisationService.GetCompanyByOrgId(record);

                        if (response == null)
                        {
                            // return Problem("Failed to create and add organisation", statusCode: StatusCodes.Status500InternalServerError);
                            _logger.LogError("Invalid Parent Record Org Id : {OrganisationId} {Organisation_Name}", record.organisation_id, record.organisation_name);
                            continue;
                        }

                        if (response != null)
                        {
                            // populdate any object for parent org
                        }

                        // question. in one CSV will organisationId (1st column) will already be same? i.e parentID
                        var childRecords = records.Where(c => c.organisation_id == record.organisation_id).ToList();

                        foreach (var subsidiaryRecord in childRecords.Where(c => c.parent_child.ToUpper() == "CHILD" && string.IsNullOrEmpty(c.franchisee_licensee_tenant)).ToList())
                        {
                            OrganisationModel subsidiary = new OrganisationModel()
                            {
                                OrganisationId = record.organisation_id,
                                SubsidiaryId = record.subsidiary_id,
                                OrganisationName = record.organisation_name,
                                CompaniesHouseNumber = record.companies_house_number,
                                ParentChild = record.parent_child,
                                FranchiseeLicenseeTenant = record.franchisee_licensee_tenant
                            };

                            // check if the subsidiary company exists in the database
                            var subsidiaryResponse = await organisationService.GetCompanyByOrgId(record);

                            if (subsidiaryResponse != null)
                            {
                                // Question for mike. why the org ids are defined as GUID and is this ever tested?
                                // company already exists in the database
                                SubsidiaryAddModel existingSubsidiary = new SubsidiaryAddModel()
                                {
                                    UserId = Guid.NewGuid(),
                                    ParentOrganisationId = subsidiaryRecord.organisation_id,
                                    ChildOrganisationId = record.organisation_id
                                };

                                var localCreateResponse = await organisationService.AddSubsidiaryRelationshipAsync(existingSubsidiary);
                                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", record.organisation_id, record.organisation_name, record.organisation_name);
                                continue;
                            }

                            // company does not exist in org database. check if subsidiary company exists in the table storage (temp data)
                            var tableStorageResponse = await organisationService.GetCompanyByOrgIdFromTableStorage(record.companies_house_number);
                            if (tableStorageResponse != null)
                            {
                                // company exists in temp storage (table storage)
                                LinkOrganisationModel newSubsidiary = new LinkOrganisationModel()
                                {
                                    UserId = Guid.NewGuid(),
                                    Subsidiary = subsidiary
                                };
                                var tableStorageCreateResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiary);
                                _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", record.organisation_id, record.organisation_name);
                                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", record.organisation_id, record.organisation_name, record.organisation_name);
                                continue;
                            }

                            // Company does not exist in table storage. make call to comapanies house API
                            var companyHouseResponse = await companiesHouseLookupService.GetCompaniesHouseResponseAsync(record.companies_house_number);
                            if (companyHouseResponse != null)
                            {
                                // company exists in companies hosue database
                                LinkOrganisationModel newSubsidiary = new LinkOrganisationModel()
                                {
                                    UserId = Guid.NewGuid(),
                                    Subsidiary = subsidiary
                                };

                                var createFromCompaniesHouseDaaResponse = await organisationService.CreateAndAddSubsidiaryAsync(newSubsidiary);
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
                                OrganisationId = record.organisation_id,
                                SubsidiaryId = record.subsidiary_id,
                                OrganisationName = record.organisation_name,
                                CompaniesHouseNumber = record.companies_house_number,
                                ParentChild = record.parent_child,
                                FranchiseeLicenseeTenant = record.franchisee_licensee_tenant
                            };

                            LinkOrganisationModel franchiseeToCreate = new LinkOrganisationModel()
                            {
                                UserId = Guid.NewGuid(),
                                Subsidiary = franchisee
                            };

                            var franchiseeCreateResponse = await organisationService.CreateAndAddSubsidiaryAsync(franchiseeToCreate);

                            if (franchiseeCreateResponse == null)
                            {
                                // return Problem("Failed to create and add franchisee", statusCode: StatusCodes.Status500InternalServerError);
                                _logger.LogInformation("Franchisee Company added to the database : {OrganisationId} {Organisation_Name}.", franchisee.OrganisationId, franchisee.OrganisationName);
                                _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", franchisee.OrganisationId, franchisee.OrganisationName, record.organisation_name);
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
