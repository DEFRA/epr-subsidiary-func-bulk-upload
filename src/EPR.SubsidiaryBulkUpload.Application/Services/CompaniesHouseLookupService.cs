using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

// using Microsoft.Extensions.Logging;
namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class CompaniesHouseLookupService : ICompaniesHouseLookupService
{
    private const string CompaniesHouseEndpoint = "CompaniesHouse/companies";

    // private readonly ILogger<CompaniesHouseService> _logger;
    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Company?> GetCompaniesHouseResponseAsync(string id)
    {
        var response = await _httpClient.GetAsync($"{CompaniesHouseEndpoint}/{id}");
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
}