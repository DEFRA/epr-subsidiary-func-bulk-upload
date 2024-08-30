using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// using Microsoft.Extensions.Logging;
namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class CompaniesHouseLookupService : ICompaniesHouseLookupService
{
    private const string CompaniesHouseEndpoint = "CompaniesHouse/companies";

    private readonly ApiOptions _apiConfig;
    private readonly ILogger<CompaniesHouseLookupService> _logger;
    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupService(HttpClient httpClient, IOptions<ApiOptions> apiOptions, ILogger<CompaniesHouseLookupService> logger)
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

            _logger.LogInformation("Has certificate? {Has} length {Len}", !string.IsNullOrEmpty(_apiConfig?.Certificate), _apiConfig?.Certificate?.Length ?? 0);

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

            // TODO: Confirm if this should be CompaniesHouseResponse or CompaniesHouseCompany - if the latter we need a new version of that class
            var company = await response.Content.ReadFromJsonAsync<CompaniesHouseResponseFromCompaniesHouse>();
            return new Company(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed {Message}", ex.Message);
            throw;
        }
    }
}