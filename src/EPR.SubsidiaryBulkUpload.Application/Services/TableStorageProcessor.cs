using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Services.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class TableStorageProcessor(
    ILogger<CompaniesHousCsvProcessor> logger) : ITableStorageProcessor
{
    private const string CurrentIngestion = "Current Ingestion";
    private const string LatestCHData = "Latest CH Data";
    private const string Latest = "Latest";
    private readonly ILogger<CompaniesHousCsvProcessor> _logger = logger;

    public async Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, string connectionString, int batchSize)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        var table = tableClient.GetTableReference(tableName);

        await table.CreateIfNotExistsAsync();

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "EmptyParttionKey";
        }

        var currentIngestion = new CompanyHouseTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = CurrentIngestion
        };

        try
        {
            var insertOperation = TableOperation.InsertOrReplace(currentIngestion);
            await table.ExecuteAsync(insertOperation);

            var batchOperation = new TableBatchOperation();

            foreach (var record in records)
            {
                record.PartitionKey = partitionKey;
                record.RowKey = record.CompanyNumber;
                batchOperation.InsertOrReplace(record);

                if (batchOperation.Count >= batchSize)
                {
                    await table.ExecuteBatchAsync(batchOperation);
                    batchOperation.Clear();
                }
            }

            if (batchOperation.Count > 0)
            {
                await table.ExecuteBatchAsync(batchOperation);
            }

            var latestData = new CompanyHouseTableEntity
            {
                PartitionKey = LatestCHData,
                RowKey = Latest,
                Data = partitionKey
            };
            var updateOperation = TableOperation.InsertOrReplace(latestData);
            await table.ExecuteAsync(updateOperation);

            var deleteOperation = TableOperation.Delete(currentIngestion);
            await table.ExecuteAsync(deleteOperation);

            _logger.LogInformation("C# Table storage processed {Count} records from csv storage table {Name}", records.Count(), tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ingestion.");

            var deleteOperation = TableOperation.Delete(currentIngestion);
            await table.ExecuteAsync(deleteOperation);
            throw;
        }
    }
}
