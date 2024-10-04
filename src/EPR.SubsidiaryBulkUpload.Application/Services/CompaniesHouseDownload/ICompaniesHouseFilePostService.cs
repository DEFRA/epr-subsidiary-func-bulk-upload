using System.Net;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public interface ICompaniesHouseFilePostService
{
    Task<HttpStatusCode> PostFileAsync(Stream stream, string fileName);
}