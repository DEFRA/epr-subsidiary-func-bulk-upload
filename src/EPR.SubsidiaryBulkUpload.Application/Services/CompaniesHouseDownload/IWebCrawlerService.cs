namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload
{
    public interface IWebCrawlerService
    {
        int GetCompaniesHouseFileDownloadCount(string downloadPagePath);
    }
}
