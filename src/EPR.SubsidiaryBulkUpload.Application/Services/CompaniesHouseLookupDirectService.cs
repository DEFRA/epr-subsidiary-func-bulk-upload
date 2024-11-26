using System.Net;
using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

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
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = jsonDocument.RootElement;

        return new Company
        {
            Name = root.GetStringFromJsonElement("company_name"),
            CompaniesHouseNumber = root.GetStringFromJsonElement("company_number"),
            Error = null,
            BusinessAddress = root.TryGetProperty("registered_office_address", out var address)
                ? new Address
                {
                    Street = address.GetStringFromJsonElement("address_line_1"),
                    Locality = address.GetStringFromJsonElement("address_line_2"),
                    County = address.GetStringFromJsonElement("locality"),
                    Country = address.GetStringFromJsonElement("country"),
                    Postcode = address.GetStringFromJsonElement("postal_code")
                }
                : new Address()
        };
    }
}
