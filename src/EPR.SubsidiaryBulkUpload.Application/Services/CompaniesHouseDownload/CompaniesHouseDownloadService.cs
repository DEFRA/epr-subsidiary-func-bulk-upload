/*using Microsoft.AspNetCore.Http;*/
using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseDownloadService(IFileDownloadService fileDownloadService,
    IDownloadStatusStorage downloadStatusStorage,
    ICompaniesHouseFilePostService companiesHouseFilePostService,
    IOptions<ApiOptions> apiOptions,
    TimeProvider timeProvider) : ICompaniesHouseDownloadService
{
    private readonly IFileDownloadService fileDownloadService = fileDownloadService;
    private readonly IDownloadStatusStorage downloadStatusStorage = downloadStatusStorage;
    private readonly ICompaniesHouseFilePostService companiesHouseFilePostService = companiesHouseFilePostService;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ApiOptions apiOptions = apiOptions.Value;

    public async Task StartDownload()
    {
        var partitionKey = timeProvider.GetUtcNow().Month.ToString();

        if (await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey))
        {
            await DownloadFiles(partitionKey);
        }
    }

    private async Task DownloadFiles(string partitionKey)
    {
        var now = timeProvider.GetUtcNow();
        await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey);

        var filesDownloadList = await downloadStatusStorage.GetCompaniesHouseFileDownloadListAsync(partitionKey);

        foreach (var fileStatus in filesDownloadList)
        {
            await DownloadFile(fileStatus, now);
        }
    }

    private async Task<bool> DownloadFile(CompaniesHouseFileSetDownloadStatus fileStatus, DateTimeOffset now)
    {
        var succeeded = false;

        var filePath = $"{apiOptions.CompaniesHouseDataDownloadUrl}{fileStatus.DownloadedFileName}";
        var download = await fileDownloadService.GetStreamAsync(filePath);

        if(download.ResponseCode == Models.FileDownloadResponseCode.Succeeded)
        {
            /*var status = await companiesHouseFilePostService.PostFileAsync(download.Stream, fileStatus.DownloadedFileName);  // TODO: */
            var status = HttpStatusCode.OK;

            succeeded = status.IsSuccessStatusCode();
            if (succeeded)
            {
                fileStatus.Timestamp = timeProvider.GetUtcNow();
                fileStatus.DownloadStatus = download.ResponseCode;

                await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(fileStatus);
            }
            else
            {
                fileStatus.Timestamp = timeProvider.GetUtcNow();
                fileStatus.DownloadStatus = download.ResponseCode;

                await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(fileStatus);
            }
        }

        return succeeded;
    }
}
