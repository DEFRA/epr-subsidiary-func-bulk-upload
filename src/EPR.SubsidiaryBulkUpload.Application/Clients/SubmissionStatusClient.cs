﻿using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
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

    public async Task<HttpStatusCode> CreateEventAsync<T>(T @event, Guid submissionId, Guid? userId = null, Guid? organisationId = null)
        where T : AbstractEvent
    {
        return await Post($"submissions/{submissionId}/events", @event, userId, organisationId);
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

    private async Task<HttpStatusCode> Post<T>(string requestUri, T data, Guid? userId = null, Guid? organisationId = null)
    {
        HttpStatusCode statusCode;

        try
        {
            ConfigureHttpClientAsync(userId, organisationId);

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

    private void ConfigureHttpClientAsync(Guid? userId = null, Guid? organisationId = null)
    {
        if(userId is not null)
        {
            AddIfMissing(_httpClient.DefaultRequestHeaders, "UserId", userId.Value.ToString());
        }
        else
        {
            var systemUserId = _systemDetailsProvider.SystemUserId?.ToString() ?? throw new MissingSystemDetailsException("System user id was not found");
            AddIfMissing(_httpClient.DefaultRequestHeaders, "UserId", systemUserId);
        }

        if (organisationId is not null)
        {
            AddIfMissing(_httpClient.DefaultRequestHeaders, "OrganisationId", organisationId.Value.ToString());
        }
        else
        {
            var systemOrganisationId = _systemDetailsProvider.SystemOrganisationId?.ToString() ?? throw new MissingSystemDetailsException("System organisation id was not found");
            AddIfMissing(_httpClient.DefaultRequestHeaders, "OrganisationId", systemOrganisationId);
        }
    }
}