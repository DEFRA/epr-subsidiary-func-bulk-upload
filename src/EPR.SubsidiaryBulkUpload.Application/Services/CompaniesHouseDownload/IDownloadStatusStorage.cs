using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public interface IDownloadStatusStorage
{
    Task<CompaniesHouseFileSetDownloadStatus?> GetCompaniesHouseFileDownloadStatusAsync();

    Task<bool> SetCompaniesHouseFileDownloadStatusAsync(CompaniesHouseFileSetDownloadStatus status);
}
