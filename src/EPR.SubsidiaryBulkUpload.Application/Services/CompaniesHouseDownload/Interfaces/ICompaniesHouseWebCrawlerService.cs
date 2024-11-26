namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces
{
    public interface ICompaniesHouseWebCrawlerService
    {
        Task<int> GetCompaniesHouseFileDownloadCount(string downloadPagePath);
    }
}
