using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

// Note, this code was cloned from WebApiGateway.
public class AntivirusClient : IAntivirusClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AntivirusClient> _logger;

    public AntivirusClient(HttpClient httpClient, ILogger<AntivirusClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream)
    {
        var result = false;
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

            response.EnsureSuccessStatusCode();

            result = true;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Error sending file to antivirus api");
        }

        return result;
    }
}