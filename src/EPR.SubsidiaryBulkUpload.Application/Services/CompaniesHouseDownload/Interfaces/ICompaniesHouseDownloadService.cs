namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;

public interface ICompaniesHouseDownloadService
{
    Task StartDownload();

    Task DownloadFiles(string partitionKey);
}
