using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class BulkUploadFunction
{
    private readonly ILogger<BulkUploadFunction> _logger;
    private readonly ISubsidiaryService _organisationService;
    private readonly ICompaniesHouseLookupService _companiesHouseLookupService;

    public BulkUploadFunction(ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService, ILogger<BulkUploadFunction> logger)
    {
        _organisationService = organisationService;
        _companiesHouseLookupService = companiesHouseLookupService;
        _logger = logger;
    }

    [Function(nameof(BulkUploadFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:SubsidiaryContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        Stream stream,
        string name)
    {
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
            var parentRecord = records.Where(c => c.parent_child.ToUpper() == "PARENT").FirstOrDefault();

            // check if the parent company exists in the database
            if (!string.IsNullOrEmpty(parentRecord.companies_house_number))
            {
                var response = await _organisationService.GetCompanyByOrgId(parentRecord);

                if (response == null)
                {
                    // return Problem("Failed to create and add organisation", statusCode: StatusCodes.Status500InternalServerError);
                    _logger.LogInformation("Invalid Parent Record Org Id : {OrganisationId} {Organisation_Name}", parentRecord.organisation_id, parentRecord.organisation_name);
                    throw new Exception("Invalid Parent Record");
                }

                if (response != null)
                {
                    // populdate any object for parent org
                }
            }

            foreach (var record in records.Where(c => c.parent_child.ToUpper() == "CHILD" && string.IsNullOrEmpty(c.franchisee_licensee_tenant)).ToList())
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
                var response = await _organisationService.GetCompanyByOrgId(record);

                if (response != null)
                {
                    // Question for mike. why the org ids are defined as GUID and is this ever tested?
                    // company already exists in the database
                    SubsidiaryAddModel existingSubsidiary = new SubsidiaryAddModel()
                    {
                        UserId = Guid.NewGuid(),
                        ParentOrganisationId = parentRecord.organisation_id,
                        ChildOrganisationId = record.organisation_id
                    };

                    var localCreateResponse = await _organisationService.AddSubsidiaryRelationshipAsync(existingSubsidiary);
                    _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", record.organisation_id, record.organisation_name, parentRecord.organisation_name);
                    continue;
                }

                // company does not exist in org database. check if subsidiary company exists in the table storage (temp data)
                var tableStorageResponse = await _organisationService.GetCompanyByOrgIdFromTableStorage(record.companies_house_number);
                if (tableStorageResponse != null)
                {
                    // company exists in temp storage (table storage)
                    LinkOrganisationModel newSubsidiary = new LinkOrganisationModel()
                    {
                        UserId = Guid.NewGuid(),
                        Subsidiary = subsidiary
                    };
                    var tableStorageCreateResponse = await _organisationService.CreateAndAddSubsidiaryAsync(newSubsidiary);
                    _logger.LogInformation("Subsidiary Company added to the database : {OrganisationId} {Organisation_Name}.", record.organisation_id, record.organisation_name);
                    _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", record.organisation_id, record.organisation_name, parentRecord.organisation_name);
                    continue;
                }

                // Company does not exist in table storage. make call to comapanies house API
                var companyHouseResponse = await _companiesHouseLookupService.GetCompaniesHouseResponseAsync(record.companies_house_number);
                if (companyHouseResponse != null)
                {
                    // company exists in companies hosue database
                    LinkOrganisationModel newSubsidiary = new LinkOrganisationModel()
                    {
                        UserId = Guid.NewGuid(),
                        Subsidiary = subsidiary
                    };

                    var createFromCompaniesHouseDaaResponse = await _organisationService.CreateAndAddSubsidiaryAsync(newSubsidiary);
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
            foreach (var record in records.Where(c => c.parent_child.ToUpper() == "CHILD" && !string.IsNullOrEmpty(c.franchisee_licensee_tenant) && c.franchisee_licensee_tenant.ToUpper() == "Y").ToList())
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

                var franchiseeCreateResponse = await _organisationService.CreateAndAddSubsidiaryAsync(franchiseeToCreate);

                if (franchiseeCreateResponse == null)
                {
                    // return Problem("Failed to create and add franchisee", statusCode: StatusCodes.Status500InternalServerError);
                    _logger.LogInformation("Franchisee Company added to the database : {OrganisationId} {Organisation_Name}.", franchisee.OrganisationId, franchisee.OrganisationName);
                    _logger.LogInformation("Subsidiary Company {OrganisationId} {Organisation_Name} linked to {CompanyName} in the database.", franchisee.OrganisationId, franchisee.OrganisationName, parentRecord.organisation_name);
                }
            }
        }

        // var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name}", name);
    }
}
