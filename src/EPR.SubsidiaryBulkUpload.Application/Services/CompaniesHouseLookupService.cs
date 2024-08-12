using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;
public class CompaniesHouseLookupService : ICompaniesHouseLookupService
{
    private const string CompaniesHouseEndpoint = "/company";
    private const string FindAndUpdateCompanyInformation = "https://find-and-update.company-information.service.gov.uk";

    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CompaniesHouseResponse?> GetCompaniesHouseResponseAsync(string id, bool isDevMode)
    {
        var response = await _httpClient.GetAsync($"{CompaniesHouseEndpoint}/{id}");
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<CompaniesHouseErrorResponse>();
            if (errorResponse?.InnerException?.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CompaniesHouseResponse>();
    }

    public async Task<Company?> GetCompaniesHouseResponseAsync(string id)
    {
        var path = $"companies-house?id={id}";
        var response = await _httpClient.GetAsync($"{FindAndUpdateCompanyInformation}/{path}");

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
