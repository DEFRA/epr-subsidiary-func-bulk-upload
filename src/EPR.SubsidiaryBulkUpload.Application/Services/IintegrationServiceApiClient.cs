namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IIntegrationServiceApiClient
{
    Task<HttpResponseMessage> SendGetRequest(string endpoint);
}