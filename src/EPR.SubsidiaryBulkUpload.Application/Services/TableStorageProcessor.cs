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
    public const string LatestCHData = "Latest CH Data";
    public const string Latest = "Latest";
    public const string ToDelete = "To Delete";
    private const int TableBatchSize = 100;

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

    public async Task<CompanyHouseTableEntity?> GetByCompanyNumber(string companiesHouseNumber, string tableName)
    {
        CompanyHouseTableEntity? companiesHouseEntity = null;

        try
        {
            var partitionKey = await GetLatestPartitionKey(tableName);

            if (partitionKey != null)
            {
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

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

    // https://medienstudio.net/development-en/delete-all-rows-from-azure-table-storage-as-fast-as-possible/
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
                    deleted += responses.Sum(response => response.Value.Count);
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

    // https://medium.com/medialesson/deleting-all-rows-from-azure-table-storage-as-fast-as-possible-79e03937c331
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
                var batch = items.Take(TableBatchSize);
                items = items.Skip(TableBatchSize);

                var actions = new List<TableTransactionAction>();
                actions.AddRange(batch.Select(e => new TableTransactionAction(tableTransactionActionType, e)));
                var response = await tableClient.SubmitTransactionAsync(actions).ConfigureAwait(false);
                responses.Add(response);
            }
        }

        return responses;
    }

    private async Task<string?> GetLatestPartitionKey(string tableName)
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();

        var tableResult = await tableClient.GetEntityAsync<CompanyHouseTableEntity>(
            partitionKey: LatestCHData,
            rowKey: Latest);

        return tableResult?.Value?.Data;
    }
}