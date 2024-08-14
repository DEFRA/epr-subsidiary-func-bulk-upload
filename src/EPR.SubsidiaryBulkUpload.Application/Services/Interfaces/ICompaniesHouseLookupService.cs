using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICompaniesHouseLookupService
{
    Task<Company?> GetCompaniesHouseResponseAsync(string id);

    // Task<Company?> GetCompaniesHouseResponseAsync(string id, bool devMode);
    // Task<Company?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber);
}