using System.Net;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;

public interface ICompaniesHouseFilePostService
{
    Task<HttpStatusCode> PostFileAsync(Stream stream, string fileName);
}