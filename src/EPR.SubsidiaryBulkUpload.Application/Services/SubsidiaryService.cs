using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class SubsidiaryService : ISubsidiaryService
{
    private const string OrganisationByCompanyHouseNumberUri = "api/bulkuploadorganisations/";
    private const string OrganisationCreateAddSubsidiaryUri = "api/bulkuploadorganisations/create-subsidiary-and-add-relationship";
    private const string OrganisationAddSubsidiaryUri = "api/bulkuploadorganisations/add-subsidiary-relationship";
    private const string OrganisationRelationshipsByIdUri = "api/bulkuploadorganisations/organisation-by-relationship";
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly HttpClient _httpClient;

    public SubsidiaryService(
        HttpClient httpClient,
        ILogger<SubsidiaryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrganisationModel?> GetCompanyByOrgId(CompaniesHouseCompany company)
    {
        var response = await _httpClient.GetAsync($"{OrganisationByCompanyHouseNumberUri}?organisation_id={company.organisation_id}");
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonWithEnumsAsync<OrganisationModel>();
    }

    public async Task<bool> GetSubsidiaryRelationshipAsync(int parentOrganisationId, int subsidiaryOrganisationId)
    {
        var response = await _httpClient.GetAsync($"{OrganisationRelationshipsByIdUri}?parentId={parentOrganisationId}&subsidiaryId={subsidiaryOrganisationId}");
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        var orgResponse = await response.Content.ReadFromJsonAsync<bool>();

        return orgResponse != null && orgResponse;
    }

    public async Task<OrganisationResponseModel?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber)
    {
        var response = await _httpClient.GetAsync($"{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={companiesHouseNumber}");
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        /*
Test method EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.SubsidiaryServiceTests.GetCompanyByCompaniesHouseNumber_ReturnsAccount threw exception:
System.Text.Json.JsonException: The JSON value could not be converted to EPR.SubsidiaryBulkUpload.Application.Models.OrganisationResponseModel[]. Path: $ | LineNumber: 0 | BytePositionInLine: 1.
        */
        response.EnsureSuccessStatusCode();
        try
        {
            var s = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(s);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            }))
            {
                jsonDocument.WriteTo(writer);
            }

            var docStr = Encoding.UTF8.GetString(stream.ToArray());

            var orgResponse = await response.Content.ReadFromJsonAsync<OrganisationResponseModel[]>();
            return orgResponse.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<string?> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel)
    {
        string json = JsonConvert.SerializeObject(linkOrganisationModel);

        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OrganisationCreateAddSubsidiaryUri, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                _logger.LogError("Failed to create and add subsidiary for Parent: {Parent} Subsidiary: {Subsidiary}", linkOrganisationModel.ParentOrganisationId, linkOrganisationModel.Subsidiary.Name);

                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    public async Task<string?> AddSubsidiaryRelationshipAsync(SubsidiaryAddModel subsidiaryAddModel)
    {
        string json = JsonConvert.SerializeObject(subsidiaryAddModel);
        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OrganisationAddSubsidiaryUri, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                _logger.LogError("Failed to add subsidiary relationship for Parent: {Parent} Subsidiary: {Subsidiary}", subsidiaryAddModel.ParentOrganisationId, subsidiaryAddModel.ChildOrganisationId);

                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        return result;
    }
}