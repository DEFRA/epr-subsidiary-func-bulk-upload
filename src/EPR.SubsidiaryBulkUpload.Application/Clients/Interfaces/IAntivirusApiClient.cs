namespace EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;

using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;

public interface IAntivirusApiClient
{
    Task SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream);
}