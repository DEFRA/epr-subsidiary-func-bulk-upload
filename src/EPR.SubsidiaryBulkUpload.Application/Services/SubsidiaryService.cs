namespace EPR.SubsidiaryBulkUpload.Application.Services;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class SubsidiaryService : ISubsidiaryService
{
    private const string CompaniesHouseNumber = "07073807";
    private const string BaseAddress = "http://localhost:5000";
    private const string ExpectedUrl = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={CompaniesHouseNumber}";
    private const string OrganisationByCompanyHouseNumberUri = "api/organisations/";
    private const string OrganisationByTableStorageUri = "api/organisations/organisation-by-tablestorage";
    private const string OrganisationUri = "api/organisations/organisation-by-externalId";
    private const string OrganisationNameUri = "api/organisations/organisation-by-invite-token";
    private const string OrganisationCreateAddSubsidiaryUri = "api/organisations/create-and-add-subsidiary";
    private const string OrganisationAddSubsidiaryUri = "api/organisations/add-subsidiary";
    private const string OrganisationRelationshipsByIdUri = "api/organisations/organisation-by-relationship";
    private readonly ILogger<SubsidiaryService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public SubsidiaryService(
        HttpClient httpClient,
        ILogger<SubsidiaryService> logger,
        IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
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

    public async Task<bool?> GetSubsidiaryRelationshipAysnc(int parentOrganisationId, int subsidiaryOrganisationId)
    {
        var response = await _httpClient.GetAsync($"{OrganisationRelationshipsByIdUri}?parentId={parentOrganisationId}&subsidiaryId={subsidiaryOrganisationId}");
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

    public async Task<OrganisationModel> GetCompanyByOrgIdFromTableStorage(string companiesHouseNumber)
    {
        List<OrganisationModel> companies = new List<OrganisationModel>();

        // TO DO to switch to service call
        // AzureStorageTableService tableService = new AzureStorageTableService("");
        // var tsResponse = await tableService.GetAll();
        // return tsResponse;
        string tableName = "testdatatable";
        var tableClient = new TableClient(_config["ApiConfig:StorageConnectionString"], tableName);
        Pageable<TableEntity> oDataQueryEntities = tableClient.Query<TableEntity>(filter: TableClient.CreateQueryFilter($"CompanyNumber eq {companiesHouseNumber}"));

        foreach (TableEntity entity in oDataQueryEntities)
        {
            var company = new OrganisationModel()
            {
                Name = entity.GetString("CompanyName"),
                CompaniesHouseNumber = entity.GetString("CompanyNumber"),
            };

            AddressModel address = new AddressModel()
            {
                County = entity.GetString("RegAddressCounty"),
                Postcode = entity.GetString("RegAddressPostCode"),
                Country = entity.GetString("RegAddressCountry"),
                Town = entity.GetString("RegAddressPostTown"),
                Street = entity.GetString("RegAddressAddressLine1")
            };

            company.Address = address;
            companies.Add(company);
        }

        return companies.FirstOrDefault();
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