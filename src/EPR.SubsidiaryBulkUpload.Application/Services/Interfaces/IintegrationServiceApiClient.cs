namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface IIntegrationServiceApiClient
{
    Task<HttpResponseMessage> SendGetRequest(string endpoint);
}