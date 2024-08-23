using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface ICompaniesHouseLookupService
{
    Task<Company?> GetCompaniesHouseResponseAsync(string id);
}