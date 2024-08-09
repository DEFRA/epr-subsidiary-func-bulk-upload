namespace EPR.SubsidiaryBulkUpload.Application.Services;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
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

    public async Task<Company> GetCompanyByOrgIdFromTableStorage(string companiesHouseNumber)
    {
        List<Company> companies = new List<Company>();
        var companytemp = new Company()
        {
            Organisation_Name = "W23 GLOBAL GP LLP",
            Companies_House_Number = "OC450849",
        };
        companies.Add(companytemp);

        return companies.FirstOrDefault();

        // AzureStorageTableService tableService = new AzureStorageTableService("");
        // var tsResponse = await tableService.GetAll();
        // return tsResponse;

        // var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
        string connectionString = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
        string tableName = "testdata";
        string pkey = "e311a8c9-e163-4e65-b02c-cf71fa439072";

        BlobContainerClient blobContainerClient = new BlobContainerClient("UseDevelopmentStorage=true", "devstoreaccount1");
        blobContainerClient.CreateIfNotExists();
        var tableClient = new TableClient(connectionString, tableName);
        Pageable<TableEntity> results = tableClient.Query<TableEntity>(entity => entity.PartitionKey == pkey);
        Pageable<TableEntity> oDataQueryEntities = tableClient.Query<TableEntity>(filter: TableClient.CreateQueryFilter($"CompanyName eq {"!NSPIRED INVESTMENTS LTD"}"));

        foreach (TableEntity entity in oDataQueryEntities)
        {
            var company = new Company()
            {
                Organisation_Name = entity.GetString("CompanyName"),
                Companies_House_Number = entity.GetString("CompanyNumber"),
            };
            companies.Add(company);
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