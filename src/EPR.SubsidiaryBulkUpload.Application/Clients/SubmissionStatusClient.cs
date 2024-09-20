using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

public class SubmissionStatusClient(
                HttpClient httpClient,
                ISystemDetailsProvider systemDetailsProvider,
                ILogger<SubmissionStatusClient> logger) : ISubmissionStatusClient
{
    private readonly ILogger<SubmissionStatusClient> _logger = logger;
    private readonly ISystemDetailsProvider _systemDetailsProvider = systemDetailsProvider;
    private readonly HttpClient _httpClient = httpClient;

    public async Task<HttpStatusCode> CreateEventAsync(AntivirusCheckEvent antivirusEvent, Guid submissionId)
    {
        return await Post($"submissions/{submissionId}/events", antivirusEvent);
    }

    public async Task<HttpStatusCode> CreateSubmissionAsync(CreateSubmission submission)
    {
        return await Post($"submissions", submission);
    }

    private static void AddIfMissing(HttpRequestHeaders headers, string key, string value)
    {
        if (!headers.Contains(key))
        {
            headers.Add(key, value);
        }
    }

    private async Task<HttpStatusCode> Post<T>(string requestUri, T data)
    {
        HttpStatusCode statusCode;

        try
        {
            ConfigureHttpClientAsync();

            var response = await _httpClient.PostAsJsonAsync<T>(requestUri, data);

            statusCode = response.StatusCode;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Error posting to service {RequestUri}", requestUri);
            statusCode = HttpStatusCode.RequestTimeout;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Error posting to service {RequestUri}", requestUri);
            statusCode = exception.StatusCode ?? HttpStatusCode.InternalServerError;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error posting to service {RequestUri}", requestUri);
            statusCode = HttpStatusCode.InternalServerError;
        }

        return statusCode;
    }

    private void ConfigureHttpClientAsync()
    {
        AddIfMissing(_httpClient.DefaultRequestHeaders, "OrganisationId", _systemDetailsProvider.SystemOrganisationId.ToString());
        AddIfMissing(_httpClient.DefaultRequestHeaders, "UserId", _systemDetailsProvider.SystemUserId.ToString());
    }
}