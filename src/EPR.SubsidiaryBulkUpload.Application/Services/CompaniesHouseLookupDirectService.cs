using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

[ExcludeFromCodeCoverage(Justification = "Intended for use by developers because the cloud solution cannot be run from local developer systems")]
public class CompaniesHouseLookupDirectService : ICompaniesHouseLookupService
{
    private const string CompaniesHouseEndpoint = "/company";
    private readonly HttpClient _httpClient;

    public CompaniesHouseLookupDirectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Company?> GetCompaniesHouseResponseAsync(string id)
    {
        var response = await _httpClient.GetAsync($"{CompaniesHouseEndpoint}/{id}");
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<CompaniesHouseErrorResponse>();
            if (errorResponse?.InnerException?.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        response.EnsureSuccessStatusCode();

        var company = await response.Content.ReadFromJsonAsync<CompaniesHouseResponse>();

        return new Company(company);
    }

    public async Task<Company?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber)
    {
        throw new NotImplementedException();
    }
}
