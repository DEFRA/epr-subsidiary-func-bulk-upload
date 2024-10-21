namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload
{
    public interface ICompaniesHouseWebCrawlerService
    {
        Task<int> GetCompaniesHouseFileDownloadCount(string downloadPagePath);
    }
}
