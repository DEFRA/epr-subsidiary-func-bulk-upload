namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload
{
    public interface IWebCrawlerService
    {
        Task<int> GetCompaniesHouseFileDownloadCount(string downloadPagePath);
    }
}
