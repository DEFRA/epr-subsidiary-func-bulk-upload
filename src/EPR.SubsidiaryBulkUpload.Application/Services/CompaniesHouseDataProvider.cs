using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CompaniesHouseDataProvider(ICompaniesHouseLookupService companiesHouseLookupService, ISubsidiaryService subsidiaryService) : ICompaniesHouseDataProvider
{
    private readonly ICompaniesHouseLookupService companiesHouseLookupService = companiesHouseLookupService;
    private readonly ISubsidiaryService subsidiaryService = subsidiaryService;

    public async Task<bool> SetCompaniesHouseData(OrganisationModel subsidiaryModel)
    {
        var dataRetrieved = false;

        // Try get locally...
        var response = await subsidiaryService.GetCompanyByOrgIdFromTableStorage(subsidiaryModel.CompaniesHouseNumber);

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
}
