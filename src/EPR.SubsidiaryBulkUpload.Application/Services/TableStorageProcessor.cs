using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class TableStorageProcessor(
    TableServiceClient tableServiceClient,
    ILogger<TableStorageProcessor> logger) : ITableStorageProcessor
{
    private const string CurrentIngestion = "Current Ingestion";
    private const string LatestCHData = "Latest CH Data";
    private const string Latest = "Latest";

    private readonly TableServiceClient _tableServiceClient = tableServiceClient;
    private readonly ILogger<TableStorageProcessor> _logger = logger;

    public async Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, string connectionString, int batchSize)
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "EmptyPartitionKey";
        }

        var currentIngestion = new CompanyHouseTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = CurrentIngestion
        };

        try
        {
            await tableClient.UpsertEntityAsync(currentIngestion);

            var batch = new List<TableTransactionAction>();

            foreach (var record in records)
            {
                record.PartitionKey = partitionKey;
                record.RowKey = record.CompanyNumber;

                batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, record));

                if (batch.Count >= batchSize)
                {
                    await tableClient.SubmitTransactionAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await tableClient.SubmitTransactionAsync(batch);
            }

            var latestData = new CompanyHouseTableEntity
            {
                PartitionKey = LatestCHData,
                RowKey = Latest,
                Data = partitionKey
            };

            await tableClient.UpsertEntityAsync(latestData);

            await tableClient.DeleteEntityAsync(currentIngestion);

            _logger.LogInformation("C# Table storage processed {Count} records from csv storage table {Name}", records.Count(), tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ingestion.");

            await tableClient.DeleteEntityAsync(currentIngestion);

            throw;
        }
    }
}
