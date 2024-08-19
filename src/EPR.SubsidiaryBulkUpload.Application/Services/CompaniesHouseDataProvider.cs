using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CompaniesHouseDataProvider(ICompaniesHouseLookupService companiesHouseLookupService, IAzureStorageTableService companiesHouseLocalStorage) : ICompaniesHouseDataProvider
{
    private readonly ICompaniesHouseLookupService companiesHouseLookupService = companiesHouseLookupService;
    private readonly IAzureStorageTableService companiesHouseLocalStorage = companiesHouseLocalStorage;

    public async Task<bool> SetCompaniesHouseData(OrganisationModel subsidiaryModel)
    {
        // Try get locally...
        // Try get remotely through CH API
        return false;
    }
}
