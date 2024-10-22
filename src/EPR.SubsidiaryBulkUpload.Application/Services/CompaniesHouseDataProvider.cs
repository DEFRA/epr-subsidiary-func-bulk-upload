using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CompaniesHouseDataProvider(ICompaniesHouseLookupService companiesHouseLookupService,
    ITableStorageProcessor tableStorageService,
    IOptions<TableStorageOptions> tableStorageOptions) : ICompaniesHouseDataProvider
{
    private readonly ICompaniesHouseLookupService companiesHouseLookupService = companiesHouseLookupService;
    private readonly ITableStorageProcessor tableStorageService = tableStorageService;
    private readonly TableStorageOptions tableStorageOptions = tableStorageOptions.Value;

    public async Task<bool> SetCompaniesHouseData(OrganisationModel subsidiaryModel)
    {
        var dataRetrieved = false;

        // Try get locally...
        var response = await GetCompanyFromTableStorage(subsidiaryModel.CompaniesHouseNumber);

        if (response != null && string.Equals(response.Name, subsidiaryModel.Name, StringComparison.OrdinalIgnoreCase))
        {
            dataRetrieved = true;
            subsidiaryModel.Address = response.Address;
            subsidiaryModel.Name = subsidiaryModel.LocalStorageName = response?.Name;
            subsidiaryModel.OrganisationType = OrganisationType.CompaniesHouseCompany;
        }
        else
        {
            // Try get remotely through CH API
            var companyHouseResponse = await companiesHouseLookupService.GetCompaniesHouseResponseAsync(subsidiaryModel.CompaniesHouseNumber);

            if (companyHouseResponse != null && companyHouseResponse.Error == null)
            {
                dataRetrieved = true;
                subsidiaryModel.Address = new AddressModel()
                {
                    BuildingNumber = companyHouseResponse.BusinessAddress?.BuildingNumber,
                    Street = companyHouseResponse.BusinessAddress?.Street,
                    Country = companyHouseResponse.BusinessAddress?.Country,
                    Locality = companyHouseResponse.BusinessAddress?.Locality,
                    Postcode = companyHouseResponse.BusinessAddress?.Postcode
                };

                subsidiaryModel.Name = companyHouseResponse.Name;
                subsidiaryModel.CompaniesHouseCompanyName = companyHouseResponse.Name;
                subsidiaryModel.LocalStorageName = response?.Name;
                subsidiaryModel.OrganisationType = OrganisationType.CompaniesHouseCompany;
            }
            else if (companyHouseResponse != null && companyHouseResponse.Error != null)
            {
                dataRetrieved = true;
                subsidiaryModel.Error = companyHouseResponse.Error;
                subsidiaryModel.Error.FileLineNumber = subsidiaryModel.FileLineNumber;
                subsidiaryModel.Error.FileContent = subsidiaryModel.RawContent;
            }
        }

        return dataRetrieved;
    }

    public async Task<OrganisationModel?> GetCompanyFromTableStorage(string companiesHouseNumber)
    {
        OrganisationModel? orgModel = null;

        var tableName = tableStorageOptions.CompaniesHouseOfflineDataTableName;
        var companiesHouseCompany = await tableStorageService.GetByCompanyNumber(companiesHouseNumber, tableName);

        if (companiesHouseCompany != null)
        {
            orgModel = new OrganisationModel()
            {
                Name = companiesHouseCompany.CompanyName,
                CompaniesHouseCompanyName = string.Empty,
                LocalStorageName = companiesHouseCompany.CompanyName,
                OrganisationType = OrganisationType.CompaniesHouseCompany,
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
