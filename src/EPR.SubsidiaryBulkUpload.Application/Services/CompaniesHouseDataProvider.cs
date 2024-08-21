using EPR.SubsidiaryBulkUpload.Application.Configs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CompaniesHouseDataProvider(ICompaniesHouseLookupService companiesHouseLookupService,
    ITableStorageProcessor tableStorageService,
    IOptions<ConfigOptions> configOptions) : ICompaniesHouseDataProvider
{
    private readonly ICompaniesHouseLookupService companiesHouseLookupService = companiesHouseLookupService;
    private readonly ITableStorageProcessor tableStorageService = tableStorageService;
    private readonly IOptions<ConfigOptions> configOptions = configOptions;

    public async Task<bool> SetCompaniesHouseData(OrganisationModel subsidiaryModel)
    {
        var dataRetrieved = false;

        // Try get locally...
        var response = await GetCompanyFromTableStorage(subsidiaryModel.CompaniesHouseNumber);

        if (response != null)
        {
            dataRetrieved = true;
            subsidiaryModel.Address = response.Address;
        }
        else
        {
            // Try get remotely through CH API
            var companyHouseResponse = await companiesHouseLookupService.GetCompaniesHouseResponseAsync(subsidiaryModel.CompaniesHouseNumber);

            if (companyHouseResponse != null)
            {
                dataRetrieved = true;
                subsidiaryModel.Address = new AddressModel()
                {
                    BuildingNumber = companyHouseResponse.BusinessAddress.BuildingNumber,
                    Street = companyHouseResponse.BusinessAddress.Street,
                    Country = companyHouseResponse.BusinessAddress.Country,
                    Locality = companyHouseResponse.BusinessAddress.Locality,
                    Postcode = companyHouseResponse.BusinessAddress.Postcode
                };

                subsidiaryModel.Name = companyHouseResponse.Name;
                subsidiaryModel.OrganisationType = OrganisationType.CompaniesHouseCompany;
            }
        }

        return dataRetrieved;
    }

    public async Task<OrganisationModel?> GetCompanyFromTableStorage(string companiesHouseNumber)
    {
        OrganisationModel? orgModel = null;

        var tableName = configOptions.Value.CompaniesHouseOfflineDataTableName;
        var companiesHouseCompany = await tableStorageService.GetByCompanyNumber(companiesHouseNumber, tableName);

        if (companiesHouseCompany != null)
        {
            orgModel = new OrganisationModel()
            {
                Name = companiesHouseCompany.CompanyName,
                CompaniesHouseNumber = companiesHouseCompany.CompanyNumber,
                Address = new AddressModel
                {
                    Street = companiesHouseCompany.RegAddressAddressLine1,
                    County = companiesHouseCompany.RegAddressCounty,
                    Postcode = companiesHouseCompany.RegAddressPostCode,
                    Town = companiesHouseCompany.RegAddressPostTown,
                    Country = companiesHouseCompany.RegAddressCountry
                }
            };
        }

        return orgModel;
    }
}
