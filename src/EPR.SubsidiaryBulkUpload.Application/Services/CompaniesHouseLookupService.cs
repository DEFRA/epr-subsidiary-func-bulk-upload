using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Constants;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class CompaniesHouseLookupService : ICompaniesHouseLookupService
{
    private readonly ILogger<CompaniesHouseLookupService> _logger;
    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupService(
        HttpClient httpClient,
        IFeatureManager featureManager,
        ILogger<CompaniesHouseLookupService> logger)
    {
        _httpClient = httpClient;

        CompaniesHouseEndpoint = featureManager.IsEnabledAsync(FeatureFlags.UseBoomiOAuth).GetAwaiter().GetResult()
            ? "companies"
            : "CompaniesHouse/companies";

        _logger = logger;
    }

    private string CompaniesHouseEndpoint { get; init; }

    public async Task<Company?> GetCompaniesHouseResponseAsync(string id)
    {
        try
        {
            _logger.LogInformation("Calling companies house api {Url}", $"{CompaniesHouseEndpoint}/{id}");

            var response = await _httpClient.GetAsync($"{CompaniesHouseEndpoint}/{Uri.EscapeDataString(id)}");

            _logger.LogInformation("Got response {Status}", response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NoContent ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.InternalServerError)
            {
                return new Company
                {
                    Error = new UploadFileErrorModel
                    {
                        FileLineNumber = 1,
                        FileContent = string.Empty,
                        Message = BulkUpdateErrors.ResourceNotFoundErrorMessage,
                        ErrorNumber = BulkUpdateErrors.ResourceNotFoundError,
                        IsError = true
                    }
                };
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<CompaniesHouseErrorResponse>();
                if (errorResponse?.InnerException?.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return new Company
                {
                    Error = new UploadFileErrorModel
                    {
                        FileLineNumber = 1,
                        FileContent = string.Empty,
                        Message = BulkUpdateErrors.ResourceNotReachableOrAllOtherPossibleErrorMessage,
                        ErrorNumber = BulkUpdateErrors.ResourceNotReachableOrAllOtherPossibleError,
                        IsError = true
                    }
                };
            }

            response.EnsureSuccessStatusCode();

            var company = await response.Content.ReadFromJsonAsync<CompaniesHouseResponse>();
            return new Company(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed {Message}", ex.Message);
            throw;
        }
    }
}