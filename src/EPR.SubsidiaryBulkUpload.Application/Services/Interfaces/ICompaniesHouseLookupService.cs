using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICompaniesHouseLookupService
{
    Task<CompaniesHouseResponse?> GetCompaniesHouseResponseAsync(string id);
}