namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public interface ICompaniesHouseDownloadService
{
    Task StartDownload();

    Task DownloadFiles(string partitionKey);
}
