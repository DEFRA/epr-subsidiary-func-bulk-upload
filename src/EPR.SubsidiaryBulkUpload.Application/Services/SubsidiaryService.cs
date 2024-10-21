using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
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
    private const string SystemUserAndOrganisationUri = "api/users/system-user-and-organisation";
    private const string OrganisationByCompanyHouseNameUri = "api/bulkuploadorganisations/organisation-by-name";

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
                _logger.LogError("Failed to add subsidiary relationship for Parent: {Detail} Subsidiary: {StatusCode}", problemDetails.Detail, response.StatusCode);
            }

            return null;
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
                _logger.LogError("Error occurred in GetSubsidiaryRelationshipAsync call: {Detail} : {StatusCode}", problemDetails.Detail, response.StatusCode);
            }

            return false;
        }

        var orgResponse = await response.Content.ReadFromJsonAsync<bool>();

        return orgResponse;
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
                _logger.LogError("Error occurred in GetCompanyByCompaniesHouseNumber call: {Detail} : {StatusCode}", problemDetails.Detail, response.StatusCode);
            }

            return null;
        }

        response.EnsureSuccessStatusCode();

        var orgResponse = await response.Content.ReadFromJsonAsync<OrganisationResponseModel[]>();
        return orgResponse.FirstOrDefault();
    }

    public async Task<OrganisationResponseModel?> GetCompanyByCompanyName(string companiesHouseName)
    {
        var response = await _httpClient.GetAsync($"{OrganisationByCompanyHouseNameUri}?companiesHouseName={companiesHouseName}");
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                _logger.LogError("Error occurred in GetCompanyByCompanyName call: {Detail} : {StatusCode}", problemDetails.Detail, response.StatusCode);
            }

            return null;
        }

        response.EnsureSuccessStatusCode();

        var orgResponse = await response.Content.ReadFromJsonAsync<OrganisationResponseModel[]>();
        return orgResponse.FirstOrDefault();
    }

    public async Task<HttpStatusCode> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel)
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
            }
        }

        return response.StatusCode;
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
            }

            return null;
        }

        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    public async Task<UserOrganisation> GetSystemUserAndOrganisation()
    {
        var response = await _httpClient.GetAsync($"{SystemUserAndOrganisationUri}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new();
        }

        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (problemDetails != null)
            {
                _logger.LogError("Error occurred in GetSystemUserAndOrganisation call: Status code {StatusCode}, details {Details}", response.StatusCode, problemDetails.Detail);
            }

            return new();
        }

        return await response.Content.ReadFromJsonAsync<UserOrganisation>();
    }
}