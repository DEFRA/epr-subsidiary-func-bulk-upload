using System.Net;
using System.Net.Http.Json;
using EPR.SubsidiaryBulkUpload.Application.Configs;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class SubsidiaryService : ISubsidiaryService
{
    private const string OrganisationByCompanyHouseNumberUri = "api/bulkuploadorganisations/";
    private const string OrganisationByTableStorageUri = "api/bulkuploadorganisations/organisation-by-tablestorage";
    private const string OrganisationUri = "api/bulkuploadorganisations/organisation-by-externalId";
    private const string OrganisationNameUri = "api/bulkuploadorganisations/organisation-by-invite-token";
    private const string OrganisationCreateAddSubsidiaryUri = "api/bulkuploadorganisations/create-subsidiary-and-add-relationship";
    private const string OrganisationAddSubsidiaryUri = "api/bulkuploadorganisations/add-subsidiary-relationship";
    private const string OrganisationRelationshipsByIdUri = "api/bulkuploadorganisations/organisation-by-relationship";
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IOptions<ConfigOptions> _configOptions;

    public SubsidiaryService(
        HttpClient httpClient,
        ILogger<SubsidiaryService> logger,
        IConfiguration config,
        IOptions<ConfigOptions> configOptions,
        ITableStorageProcessor azureTableStorageService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
        _configOptions = configOptions;
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

    public async Task<bool> GetSubsidiaryRelationshipAysnc(int parentOrganisationId, int subsidiaryOrganisationId)
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

        var orgResponse = response.Content.ReadFromJsonAsync<bool>();

        if (orgResponse == null || orgResponse.Result == false)
        {
            return false;
        }

        return true;
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

        response.EnsureSuccessStatusCode();
        var orgResponse = response.Content.ReadFromJsonAsync<OrganisationResponseModel[]>();
        return orgResponse.Result.ToList().FirstOrDefault();
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
                throw new ProblemResponseException(problemDetails, response.StatusCode);
            }
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        return result;
    }
}