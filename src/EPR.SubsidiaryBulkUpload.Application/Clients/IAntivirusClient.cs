using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

// Note, this code was cloned from WebApiGateway.
public interface IAntivirusClient
{
    Task SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream);
}