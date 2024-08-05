namespace EPR.SubsidiaryBulkUpload.Application.Clients;

using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using Interfaces;
using Microsoft.Extensions.Logging;

public class AntivirusApiClient : IAntivirusApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AntivirusApiClient> _logger;

    public AntivirusApiClient(HttpClient httpClient, ILogger<AntivirusApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendFileAsync(FileDetails fileDetails, string fileName, Stream fileStream)
    {
        try
        {
            var formContent = new MultipartFormDataContent
            {
                { new StringContent(JsonSerializer.Serialize(fileDetails)), nameof(fileDetails) },
                { new StreamContent(fileStream), nameof(fileStream), fileName }
            };

            var boundary = formContent.Headers.ContentType.Parameters.First(o => o.Name == "boundary");
            boundary.Value = boundary.Value.Replace("\"", string.Empty);

            var response = await _httpClient.PutAsync($"files/stream/{fileDetails.Collection}/{fileDetails.Key}", formContent);

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Error sending file to antivirus api");
            throw;
        }
    }
}