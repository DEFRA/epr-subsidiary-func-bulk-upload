﻿using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Configs;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// using Microsoft.Extensions.Logging;
namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class CompaniesHouseLookupService : ICompaniesHouseLookupService
{
    private const string CompaniesHouseEndpoint = "CompaniesHouse/companies";

    private readonly ApiConfig _apiConfig;
    private readonly ILogger<CompaniesHouseLookupService> _logger;
    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupService(HttpClient httpClient, IOptions<ApiConfig> apiOptions, ILogger<CompaniesHouseLookupService> logger)
    {
        _apiConfig = apiOptions.Value;
        _httpClient = httpClient;

        _logger = logger;
    }

    public async Task<Company?> GetCompaniesHouseResponseAsync(string id)
    {
        try
        {
            _logger.LogInformation("Calling companies house api {Url}", $"{CompaniesHouseEndpoint}/{id}");
            if (_httpClient.DefaultRequestHeaders.Authorization != null)
            {
                _logger.LogInformation("Has auth header {Scheme}", _httpClient.DefaultRequestHeaders.Authorization.Scheme);
            }

            _logger.LogInformation("Has certificate? {Has} length {Len}", !string.IsNullOrEmpty(_apiConfig.Certificate), _apiConfig.Certificate?.Length ?? 0);

            var response = await _httpClient.GetAsync($"{CompaniesHouseEndpoint}/{id}");

            _logger.LogInformation("Got response {Status}", response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<CompaniesHouseErrorResponse>();
                if (errorResponse?.InnerException?.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            response.EnsureSuccessStatusCode();
            var company = await response.Content.ReadFromJsonAsync<CompaniesHouseCompany>();
            return new Company(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed {Message}", ex.Message);
            throw;
        }
    }
}