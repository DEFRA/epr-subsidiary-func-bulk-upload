using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class OrganisationService : IOrganisationService
{
    private const string SyncStagingOrganisationDataUri = "api/organisations/sync-staging-organisation-data";
    private readonly ILogger<OrganisationService> _logger;
    private readonly HttpClient _httpClient;

    public OrganisationService(HttpClient httpClient, ILogger<OrganisationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SyncOrganisationStagingToAccountsModel?> SyncStagingToAccounts()
    {
        var response = await _httpClient.GetAsync(SyncStagingOrganisationDataUri);
        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            if (problemDetails != null)
            {
                _logger.LogError("Failed to sync from Staging table to Accounts DB: {Detail}. StatusCode: {StatusCode}", problemDetails.Detail, response.StatusCode);
            }

            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SyncOrganisationStagingToAccountsModel>();
    }
}