using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class AzureStorageTableService : IAzureStorageTableService
    {
        private readonly TableClient _tableClient;

        public AzureStorageTableService(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient("testdata");
        }

        public async Task<List<Company>> GetAll()
        {
            List<Company> companies = new List<Company>();
            var tableResult = _tableClient.QueryAsync<CompanyHouseTableEntity>(filter: e => e.CompanyNumber == "3234234");

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
