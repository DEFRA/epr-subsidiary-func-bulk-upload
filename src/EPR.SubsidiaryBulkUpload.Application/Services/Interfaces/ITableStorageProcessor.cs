using EPR.SubsidiaryBulkUpload.Application.Services.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ITableStorageProcessor
{
    Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, string connectionString, int batchSize);
}
