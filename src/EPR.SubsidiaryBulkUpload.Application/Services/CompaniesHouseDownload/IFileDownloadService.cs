using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload
{
    public interface IFileDownloadService
    {
        Task<(Stream? Stream, FileDownloadResponseCode ResponseCode)> GetStreamAsync(string path, CancellationToken cancellation = default);
    }
}