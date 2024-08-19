using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class AzureStorageTableService : IAzureStorageTableService
    {
        private readonly TableClient _tableClient;
        private readonly string tableName = "testdata";

        // var tableClient = new TableClient(_config["ApiConfig:StorageConnectionString"], tableName);
        public AzureStorageTableService(TableServiceClient tableServiceClient, IConfiguration config)
        {
            _tableClient = tableServiceClient.GetTableClient(config["ApiConfig:StorageConnectionString"]);
        }

        public async Task<List<Company>> GetAll()
        {
            List<Company> companies = new List<Company>();
            var tableResult = _tableClient.QueryAsync<CompanyHouseTableEntity>();

            await foreach (var entity in tableResult)
            {
                var company = new Company()
                {
                    Name = entity.CompanyName,
                    CompaniesHouseNumber = entity.CompanyNumber
                };
                companies.Add(company);
            }

            return companies;
        }

        public async Task<OrganisationModel?> GetByCompanyNumber(string companiesHouseNumber)
        {
            string partitionKey = string.Empty;

            var tableResult = _tableClient.QueryAsync<CompanyHouseTableEntity>(filter: e => e.CompanyNumber == companiesHouseNumber).FirstOrDefaultAsync();
            var company = new OrganisationModel()
            {
                Name = tableResult.Result.CompanyName,
                CompaniesHouseNumber = tableResult.Result.CompanyNumber,
            };

            AddressModel address = new AddressModel()
            {
                Street = tableResult.Result.RegAddressAddressLine1,
                County = tableResult.Result.RegAddressCounty,
                Postcode = tableResult.Result.RegAddressPostCode,
                Town = tableResult.Result.RegAddressPostTown,
                Country = tableResult.Result.RegAddressCountry
            };

            company.Address = address;
            return company;
        }

        public async Task<Company> GetById(string id)
        {
            string partitionKey = string.Empty;

            var tableResult = await _tableClient.GetEntityAsync<TableEntity>(partitionKey, id);
            var company = new Company()
            {
                Name = tableResult.Value.GetString("CompanyName"),
                CompaniesHouseNumber = tableResult.Value.GetString("CompanyNumber"),
            };

            return company;
        }
    }
}
