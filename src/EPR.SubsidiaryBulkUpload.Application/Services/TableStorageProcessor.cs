using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Services.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class TableStorageProcessor(
    ILogger<CsvProcessor> logger) : ITableStorageProcessor
{
    private const string CurrentIngestion = "Current Ingestion";
    private const string CSVHeader = "CSV Header";
    private const string LatestCHData = "Latest CH Data";
    private const string Latest = "Latest";
    private readonly ILogger<CsvProcessor> _logger = logger;

    public async Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, string connectionString, int batchSize)
    {
        // Connect to the local Azure Table Storage
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        var table = tableClient.GetTableReference(tableName);

        await table.CreateIfNotExistsAsync();

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "EmptyParttionKey";
        }

        // Step 2: Set the Latest CH Data -> Current Ingestion
        var currentIngestion = new CompanyHouseTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = CurrentIngestion
        };

        try
        {
            var insertOperation = TableOperation.InsertOrReplace(currentIngestion);
            await table.ExecuteAsync(insertOperation);

            bool isHeader = true;
            var batchOperation = new TableBatchOperation();

            // Insert records into the table
            foreach (var record in records)
            {
                if (isHeader)
                {
                    record.PartitionKey = partitionKey;
                    record.RowKey = CSVHeader;
                    batchOperation.InsertOrReplace(record);
                    isHeader = false;
                }
                else
                {
                    record.PartitionKey = partitionKey;
                    record.RowKey = record.CompanyNumber;
                    batchOperation.InsertOrReplace(record);

                    // Execute batch when it reaches the batch size limit
                    if (batchOperation.Count >= batchSize)
                    {
                        await table.ExecuteBatchAsync(batchOperation);
                        batchOperation.Clear();
                    }
                }
            }

            // Execute any remaining entities in the batch
            if (batchOperation.Count > 0)
            {
                await table.ExecuteBatchAsync(batchOperation);
            }

            // Step 5: Update Latest CH Data -> Latest and Clean Up
            var latestData = new CompanyHouseTableEntity
            {
                PartitionKey = LatestCHData,
                RowKey = Latest
            };
            var updateOperation = TableOperation.InsertOrReplace(latestData);
            await table.ExecuteAsync(updateOperation);

            // Clean up Current Ingestion entry
            var deleteOperation = TableOperation.Delete(currentIngestion);
            await table.ExecuteAsync(deleteOperation);

            _logger.LogInformation("C# Table storage processed {Count} records from csv storage table {Name}", records.Count(), tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ingestion.");

            // Clean up Current Ingestion entry in case of error
            var deleteOperation = TableOperation.Delete(currentIngestion);
            await table.ExecuteAsync(deleteOperation);
            throw;
        }
    }
}
