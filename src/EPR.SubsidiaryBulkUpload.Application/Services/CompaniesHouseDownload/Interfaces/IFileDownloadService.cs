using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces
{
    public interface IFileDownloadService
    {
        Task<(Stream? Stream, FileDownloadResponseCode ResponseCode)> GetStreamAsync(string path, CancellationToken cancellation = default);
    }
}