using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class TableStorageProcessor(
    TableServiceClient tableServiceClient,
    ILogger<TableStorageProcessor> logger) : ITableStorageProcessor
{
    public const string CurrentIngestion = "Current Ingestion";
    public const string EmptyPartitionKey = "EmptyPartitionKey";
    public const string LatestCHData = "Latest CH Data";
    public const string Latest = "Latest";
    public const string Previous = "Previous";
    public const string ToDelete = "To Delete";
    private const int BatchSize = 100; // Maximum batch size for Azure Table Storage

    private readonly TableServiceClient _tableServiceClient = tableServiceClient;
    private readonly ILogger<TableStorageProcessor> _logger = logger;

    public async Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey)
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = EmptyPartitionKey;
        }

        var currentIngestion = new CompanyHouseTableEntity
        {
            PartitionKey = LatestCHData,
            RowKey = CurrentIngestion,
            Data = partitionKey
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

                if (batch.Count >= BatchSize)
                {
                    await tableClient.SubmitTransactionAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await tableClient.SubmitTransactionAsync(batch);
            }

            await CleanupIngestionTable(tableClient);

            _logger.LogInformation("C# Table storage processed {Count} records from csv storage table {Name}", records.Count(), tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ingestion.");
            await tableClient.DeleteEntityAsync(currentIngestion);

            throw;
        }
    }

    public async Task<CompanyHouseTableEntity?> GetByCompanyNumber(string companiesHouseNumber, string tableName)
    {
        CompanyHouseTableEntity? companiesHouseEntity = null;

        try
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();

            var partitionKey = await GetLatestPartitionKey(tableClient);

            if (partitionKey != null)
            {
                var result = tableClient.QueryAsync<CompanyHouseTableEntity>(
                    filter: e => e.PartitionKey == partitionKey && e.RowKey == companiesHouseNumber);

                companiesHouseEntity = await result.SingleOrDefaultAsync();
            }
        }
        catch (Exception ex)
        {
            // note: do not rethrow. The CH API will be used instead!
            _logger.LogError(ex, "An error occurred whilst retrieving a companies house details ");
        }

        return companiesHouseEntity;
    }

    public async Task<int> DeleteByPartitionKey(string tableName, string partitionKey)
    {
        var deleted = 0;

        try
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            var entities = tableClient
                .QueryAsync<CompanyHouseTableEntity>(
                    filter: e => e.PartitionKey == partitionKey,
                    select: new List<string> { "PartitionKey", "RowKey" },
                    maxPerPage: 1000);

            await entities.AsPages()
                .ForEachAwaitAsync(async page =>
                {
                    var responses = await BatchManipulateEntities(tableClient, page.Values, TableTransactionActionType.Delete).ConfigureAwait(false);
                    deleted += responses.Sum(response => response?.Value?.Count ?? 0);
                });
        }
        catch (RequestFailedException fex)
        {
            _logger.LogError(
                fex,
                "DeleteByPartitionKey: error for table '{TableName}' partition key '{PartitionKey}'. Returning 0 results.",
                tableName,
                partitionKey);
        }

        return deleted;
    }

    private static async Task<List<Response<IReadOnlyList<Response>>>> BatchManipulateEntities<TEntity>(
            TableClient tableClient,
            IEnumerable<TEntity> entities,
            TableTransactionActionType tableTransactionActionType)
        where TEntity : class, ITableEntity, new()
    {
        var groups = entities.GroupBy(x => x.PartitionKey);
        var responses = new List<Response<IReadOnlyList<Response>>>();
        foreach (var group in groups)
        {
            var items = group.AsEnumerable();
            while (items.Any())
            {
                var batch = items.Take(BatchSize);
                items = items.Skip(BatchSize);

                var actions = new List<TableTransactionAction>();
                actions.AddRange(batch.Select(e => new TableTransactionAction(tableTransactionActionType, e)));
                var response = await tableClient.SubmitTransactionAsync(actions).ConfigureAwait(false);
                responses.Add(response);
            }
        }

        return responses;
    }

    private static async Task<string?> GetPartitionRowValue(TableClient tableClient, string partitionKey, string rowKey)
    {
        try
        {
            var tableResult = await tableClient.GetEntityAsync<CompanyHouseTableEntity>(
                 partitionKey: partitionKey,
                 rowKey: rowKey);

            return tableResult?.Value?.Data;
        }
        catch (RequestFailedException)
        {
            return null;
        }

        return null;
    }

    private static async Task UpsertPartitionRowValue(TableClient tableClient, string partitionKey, string rowKey, string value)
    {
        await tableClient.UpsertEntityAsync(new CompanyHouseTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Data = value
        });
    }

    private static async Task<string?> GetLatestPartitionKey(TableClient tableClient)
    {
        return await GetPartitionRowValue(tableClient, LatestCHData, Latest);
    }

    private async Task<int> CleanupIngestionTable(TableClient tableClient)
    {
        var deletedRecordsCount = 0;

        var currentPartitionValue = await GetPartitionRowValue(tableClient, LatestCHData, CurrentIngestion);
        var latestPartitionValue = await GetPartitionRowValue(tableClient, LatestCHData, Latest);
        var previousPartitionValue = await GetPartitionRowValue(tableClient, LatestCHData, Previous);
        var toDeletePartitionValue = await GetPartitionRowValue(tableClient, LatestCHData, ToDelete);

        if (toDeletePartitionValue is not null)
        {
            deletedRecordsCount = await DeleteByPartitionKey(tableClient.Name, toDeletePartitionValue);

            await tableClient.DeleteEntityAsync(new CompanyHouseTableEntity
            {
                PartitionKey = LatestCHData,
                RowKey = ToDelete
            });

            _logger.LogInformation("C# Table storage deleted {DeletedCount} records from csv storage table {Name}", deletedRecordsCount, tableClient.Name);
        }

        if (previousPartitionValue is not null)
        {
            await UpsertPartitionRowValue(tableClient, LatestCHData, ToDelete, previousPartitionValue);
        }

        if (latestPartitionValue is not null)
        {
            await UpsertPartitionRowValue(tableClient, LatestCHData, Previous, latestPartitionValue);
        }

        if (currentPartitionValue is not null)
        {
            await UpsertPartitionRowValue(tableClient, LatestCHData, Latest, currentPartitionValue);
        }

        await tableClient.DeleteEntityAsync(new CompanyHouseTableEntity
        {
            PartitionKey = LatestCHData,
            RowKey = CurrentIngestion
        });

        return deletedRecordsCount;
    }
}