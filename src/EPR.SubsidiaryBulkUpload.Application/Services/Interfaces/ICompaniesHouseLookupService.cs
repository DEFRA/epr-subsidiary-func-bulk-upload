using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICompaniesHouseLookupService
{
    Task<Company?> GetCompaniesHouseResponseAsync(string id);
}