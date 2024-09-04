namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseDownloadService(IFileDownloadService fileDownloadService,
    IDownloadStatusStorage downloadStatusStorage,
    TimeProvider timeProvider) : ICompaniesHouseDownloadService
{
    public const string PartialFilename = "BasicCompanyData";
    public const string FileSource = "https://download.companieshouse.gov.uk/";

    private readonly IFileDownloadService fileDownloadService = fileDownloadService;
    private readonly IDownloadStatusStorage downloadStatusStorage = downloadStatusStorage;
    private readonly TimeProvider timeProvider = timeProvider;

    public async Task<bool> StartDownload()
    {
        var downloadStatus = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        return downloadStatus != null && downloadStatus.CurrentRunExpectedFileCount != null
            ? await DownloadFiles(downloadStatus.CurrentRunExpectedFileCount.Value)
            : false;
    }

    private async Task<bool> DownloadFiles(int currentRunExpectedFileCount)
    {
        var now = timeProvider.GetUtcNow();

        var month = now.Month;
        var year = now.Year;

        var all = Enumerable.Range(1, currentRunExpectedFileCount)
            .Select(i => DownloadFile(i, now));

        await Task.WhenAll(all);

        return !all.Any(t => t.Result == false);
    }

    private async Task<bool> DownloadFile(int fileCount, DateTimeOffset now)
    {
        var succeeded = false;
        var fileName = $"{PartialFilename}-{now.Year}-{now.Month}-01-part_{fileCount}.zip";
        var filePath = $"{FileSource}{fileName}";

        var download = await fileDownloadService.GetStreamAsync(filePath);

        if(download.ResponseCode == Models.FileDownloadResponseCode.Succeeded)
        {
            succeeded = await PublishToAntiVirus(download.Stream, fileName);
        }

        return succeeded;
    }

    private async Task<bool> PublishToAntiVirus(Stream fileStream, string fileName)
    {
        return false;
    }
}
