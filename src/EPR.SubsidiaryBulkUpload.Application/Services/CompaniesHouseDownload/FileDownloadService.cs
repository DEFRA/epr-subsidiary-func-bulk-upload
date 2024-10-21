using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class FileDownloadService(HttpClient httpClient, ILogger<FileDownloadService> logger) : IFileDownloadService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<FileDownloadService> logger = logger;

    public async Task<(Stream? Stream, FileDownloadResponseCode ResponseCode)> GetStreamAsync(string path, CancellationToken cancellation = default)
    {
        Stream stream = null;
        var responseCode = FileDownloadResponseCode.DownloadCancelled;

        if (!string.IsNullOrEmpty(path))
        {
            logger.LogInformation("Attempting to download {FilePath}", path);
            try
            {
                if (!cancellation.IsCancellationRequested)
                {
                    stream = await httpClient.GetStreamAsync(path, cancellation);
                    responseCode = FileDownloadResponseCode.Succeeded;
                }
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to download file, Not a valid URL {FilePath}", path);
                responseCode = FileDownloadResponseCode.InvalidFilePathUrl;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to find or connect to the download service (file may not exist) {FilePath}", path);
                responseCode = FileDownloadResponseCode.FailedToFindFile;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "File download timed out {FilePath}", path);
                responseCode = FileDownloadResponseCode.DownloadTimedOut;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "File download cancelled {FilePath}", path);
                responseCode = FileDownloadResponseCode.DownloadCancelled;
            }
        }

        return (stream, responseCode);
    }

    public async Task<int> GetCompaniesHouseFileDownloadCount(string path)
    {
        httpClient.GetStreamAsync(path);
        return 7;
    }
}
