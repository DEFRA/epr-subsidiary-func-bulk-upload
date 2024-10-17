using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseDownloadService(IFileDownloadService fileDownloadService,
    IDownloadStatusStorage downloadStatusStorage,
    ICompaniesHouseFilePostService companiesHouseFilePostService,
    IWebCrawlerService webCrawlerService,
    IOptions<ApiOptions> apiOptions,
    TimeProvider timeProvider) : ICompaniesHouseDownloadService
{
    private readonly IFileDownloadService _fileDownloadService = fileDownloadService;
    private readonly IDownloadStatusStorage _downloadStatusStorage = downloadStatusStorage;
    private readonly ICompaniesHouseFilePostService _companiesHouseFilePostService = companiesHouseFilePostService;
    private readonly IWebCrawlerService _webCrawlerService = webCrawlerService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    public async Task StartDownload()
    {
        var now = _timeProvider.GetUtcNow();
        var partitionKey = now.ToString("yyyyMM");

        if (await _downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey))
        {
            await DownloadFiles(partitionKey);
    }
    }

    private async Task DownloadFiles(string partitionKey)
    {
        var now = _timeProvider.GetUtcNow();
        var expectedFileCount = _webCrawlerService.GetCompaniesHouseFileDownloadCount(_apiOptions.CompaniesHouseDataDownloadUrl);
        await _downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey);

        var filesDownloadList = await _downloadStatusStorage.GetCompaniesHouseFileDownloadListAsync(partitionKey);
        filesDownloadList = filesDownloadList.Where(x => x.DownloadStatus != FileDownloadResponseCode.Succeeded).ToList();

        foreach (var fileStatus in filesDownloadList)
        {
            await DownloadFile(fileStatus);
        }
    }

    private async Task<bool> DownloadFile(CompaniesHouseFileSetDownloadStatus fileStatus)
    {
        var succeeded = false;
        var filePath = $"{_apiOptions.CompaniesHouseDataDownloadUrl}{fileStatus.DownloadFileName}";
        var download = await _fileDownloadService.GetStreamAsync(filePath);

        if (download.ResponseCode == FileDownloadResponseCode.Succeeded)
        {
            var status = await _companiesHouseFilePostService.PostFileAsync(download.Stream, fileStatus.DownloadFileName);
            succeeded = status.IsSuccessStatusCode();

            fileStatus.DownloadStatus = succeeded ? FileDownloadResponseCode.Succeeded : FileDownloadResponseCode.UploadFailed;
            fileStatus.Timestamp = _timeProvider.GetUtcNow();
            await _downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(fileStatus);
        }
        else
        {
            fileStatus.Timestamp = _timeProvider.GetUtcNow();
            fileStatus.DownloadStatus = download.ResponseCode;
            await _downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(fileStatus);
        }

        return succeeded;
    }
}
