using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;

namespace EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;

// Note, this code was cloned from WebApiGateway.
public interface IAntivirusClient
{
    Task<HttpStatusCode> SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream);
}