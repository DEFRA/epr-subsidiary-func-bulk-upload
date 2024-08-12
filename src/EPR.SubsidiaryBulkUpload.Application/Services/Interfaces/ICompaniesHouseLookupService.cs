using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICompaniesHouseLookupService
{
    Task<Company?> GetCompaniesHouseResponseAsync(string id);

    Task<CompaniesHouseResponse?> GetCompaniesHouseResponseAsync(string id, bool isDevMode);
}