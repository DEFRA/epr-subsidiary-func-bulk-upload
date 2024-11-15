using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

// Note, this code was cloned from WebApiGateway.
public class AntivirusClient(HttpClient httpClient, ILogger<AntivirusClient> logger) : IAntivirusClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AntivirusClient> _logger = logger;

    public async Task<HttpStatusCode> SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream)
    {
        HttpStatusCode statusCode;

        try
        {
            var formContent = new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(fileDetails)), nameof(fileDetails) },
                { new StreamContent(fileStream), nameof(fileStream), fileName }
            };

            var boundary = formContent.Headers.ContentType.Parameters.First(o => o.Name == "boundary");
            boundary.Value = boundary.Value.Replace("\"", string.Empty);

            var response = await _httpClient.PutAsync($"files/stream/{fileDetails.Collection}/{fileDetails.Key}", formContent);

            statusCode = response.StatusCode;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Timeout whilst sending file {Filename} to antivirus api", fileName);
            statusCode = HttpStatusCode.RequestTimeout;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Error sending file {FileName} to antivirus api", fileName);
            statusCode = exception.StatusCode ?? HttpStatusCode.InternalServerError;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error sending file {Filename} to antivirus api", fileName);
            statusCode = HttpStatusCode.InternalServerError;
        }

        return statusCode;
    }
}