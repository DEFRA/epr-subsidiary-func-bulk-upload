using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseDownloadService(IFileDownloadService fileDownloadService,
    IDownloadStatusStorage downloadStatusStorage,
    ICompaniesHouseFilePostService companiesHouseFilePostService,
    IOptions<ApiOptions> apiOptions,
    TimeProvider timeProvider) : ICompaniesHouseDownloadService
{
    public const string PartialFilename = "BasicCompanyData";

    private readonly IFileDownloadService fileDownloadService = fileDownloadService;
    private readonly IDownloadStatusStorage downloadStatusStorage = downloadStatusStorage;
    private readonly ICompaniesHouseFilePostService companiesHouseFilePostService = companiesHouseFilePostService;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ApiOptions apiOptions = apiOptions.Value;

    public async Task<bool> StartDownload()
    {
        var downloadStatus = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        return downloadStatus != null &&
            downloadStatus.CurrentRunExpectedFileCount != null &&
            await DownloadFiles(downloadStatus.CurrentRunExpectedFileCount.Value);
    }

    private async Task<bool> DownloadFiles(int currentRunExpectedFileCount)
    {
        var now = timeProvider.GetUtcNow();

        var all = Enumerable.Range(1, currentRunExpectedFileCount)
            .Select(i => DownloadFile(currentRunExpectedFileCount, i, now));

        var results = await Task.WhenAll(all);

        return Array.TrueForAll(results, r => r);
    }

    private async Task<bool> DownloadFile(int fileCount, int fileNumber, DateTimeOffset now)
    {
        var succeeded = false;
        var fileName = $"{PartialFilename}-{now.Year}-{now.Month.ToString("00")}-01-part{fileNumber}_{fileCount}.zip";

        var filePath = $"{apiOptions.CompaniesHouseDataDownloadUrl}{fileName}";

        var download = await fileDownloadService.GetStreamAsync(filePath);

        if(download.ResponseCode == Models.FileDownloadResponseCode.Succeeded)
        {
            var status = await companiesHouseFilePostService.PostFileAsync(download.Stream, fileName);

            succeeded = status.IsSuccessStatusCode();
        }

        return succeeded;
    }
}
