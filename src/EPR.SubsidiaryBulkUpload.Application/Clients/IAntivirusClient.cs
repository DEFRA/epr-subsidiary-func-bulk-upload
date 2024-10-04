using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

public interface IAntivirusClient
{
    Task<HttpStatusCode> SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream);
}