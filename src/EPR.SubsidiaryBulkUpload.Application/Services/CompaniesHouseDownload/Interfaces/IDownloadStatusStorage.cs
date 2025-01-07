using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;

public interface IDownloadStatusStorage
{
    Task<bool> GetCompaniesHouseFileDownloadStatusAsync(string partitionKey);

    Task<List<CompaniesHouseFileSetDownloadStatus>> GetCompaniesHouseFileDownloadListAsync(string partitionKey);

    Task<bool> SetCompaniesHouseFileDownloadStatusAsync(CompaniesHouseFileSetDownloadStatus status);

    Task CreateCompaniesHouseFileDownloadLogAsync(string partitionKey, int expectedFileCount);
}
