namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public interface ICompaniesHouseDownloadService
{
    Task<bool> StartDownload();
}
