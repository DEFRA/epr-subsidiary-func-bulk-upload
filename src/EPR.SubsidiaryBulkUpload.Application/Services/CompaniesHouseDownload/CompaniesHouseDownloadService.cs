using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseDownloadService(IFileDownloadService fileDownloadService,
    IDownloadStatusStorage downloadStatusStorage,
    ICompaniesHouseFilePostService companiesHouseFilePostService,
    ICompaniesHouseWebCrawlerService companiesHouseWebCrawlerService,
    IOptions<CompaniesHouseDownloadOptions> downloadOptions,
    TimeProvider timeProvider) : ICompaniesHouseDownloadService
{
    private readonly IFileDownloadService _fileDownloadService = fileDownloadService;
    private readonly IDownloadStatusStorage _downloadStatusStorage = downloadStatusStorage;
    private readonly ICompaniesHouseFilePostService _companiesHouseFilePostService = companiesHouseFilePostService;
    private readonly ICompaniesHouseWebCrawlerService _companiesHouseWebCrawlerService = companiesHouseWebCrawlerService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly CompaniesHouseDownloadOptions _downloadOptions = downloadOptions.Value;

    public async Task StartDownload()
    {
        var now = _timeProvider.GetUtcNow();
        var partitionKey = now.ToString("yyyyMM");

        if (await _downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey))
        {
            await DownloadFiles(partitionKey);
        }
    }

    public async Task DownloadFiles(string partitionKey)
    {
        var downloadUrl = _downloadOptions.DataDownloadUrl.TrimEnd('/');
        var downloadPage = _downloadOptions.DownloadPage.TrimStart('/');
        var downloadPath = string.Format($"{downloadUrl}/{downloadPage}");
        var expectedFileCount = await _companiesHouseWebCrawlerService.GetCompaniesHouseFileDownloadCount(downloadPath);

        if (expectedFileCount == 0)
        {
            return;
        }

        await _downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey, expectedFileCount);

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
        var filePath = $"{_downloadOptions.DataDownloadUrl}{fileStatus.DownloadFileName}";
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
