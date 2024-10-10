using System.Text.Json;
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
    public const string CurrentIngestionFileParts = "Current Ingestion File Parts";
    public const string EmptyPartitionKey = "EmptyPartitionKey";
    public const string LatestCompaniesHouseData = "Latest CH Data";
    public const string Latest = "Latest";
    public const string Previous = "Previous";
    public const string ToDelete = "To Delete";
    private const int BatchSize = 100; // Maximum batch size for Azure Table Storage

    private readonly TableServiceClient _tableServiceClient = tableServiceClient;
    private readonly ILogger<TableStorageProcessor> _logger = logger;

    public async Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, int filePart = 0, int totalFiles = 0)
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = EmptyPartitionKey;
        }

        var currentIngestion = CreateCurrentIngestion(partitionKey);
        var currentFilePart = CreateCurrentIngestionFilePart(partitionKey, filePart, totalFiles);

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

            if (currentFilePart is not null)
            {
                await tableClient.UpsertEntityAsync(currentFilePart);
            }

            await CleanupIngestionTable(tableClient);

            _logger.LogInformation("Table storage processed {Count} records into storage table {Name}", records.Count(), tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during ingestion.");
            await tableClient.DeleteEntityAsync(currentIngestion);
            if (currentFilePart is not null)
            {
                await tableClient.DeleteEntityAsync(currentFilePart);
            }

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
            _logger.LogError(ex, "An error occurred whilst retrieving companies house details.");
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

    private static CompanyHouseTableEntity CreateCurrentIngestion(string partitionKey) =>
        new()
        {
            PartitionKey = LatestCompaniesHouseData,
            RowKey = CurrentIngestion,
            Data = partitionKey
        };

    private static CompanyHouseTableEntity? CreateCurrentIngestionFilePart(string partitionKey, int filePart, int totalFiles) =>
        filePart > 0 && totalFiles > 0
            ? new()
            {
                PartitionKey = CurrentIngestionFileParts,
                RowKey = $"part{filePart}_{totalFiles}",
                Data = JsonSerializer.Serialize(new FilePart(filePart, totalFiles, partitionKey))
            }
            : null;

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
        return await GetPartitionRowValue(tableClient, LatestCompaniesHouseData, Latest);
    }

    private static async Task<bool> AllFilePartsCompleted(TableClient tableClient)
    {
        var allFileParts = await tableClient
            .QueryAsync<CompanyHouseTableEntity>(filter: e => e.PartitionKey == CurrentIngestionFileParts)
            .Select(x => JsonSerializer.Deserialize<FilePart>(x.Data))
            .ToListAsync();

        if (allFileParts.Count > 0)
        {
            var groups = allFileParts.GroupBy(x => x.TotalFiles);
            foreach (var group in groups)
            {
                var total = group.Key;
                int[] all = [.. Enumerable.Range(1, total)];
                var items = group.AsEnumerable();
                var allFound = Array.TrueForAll(all, a => items.Any(x => x.PartNumber == a));
                if (!allFound)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<int> CleanupIngestionTable(TableClient tableClient)
    {
        if (!(await AllFilePartsCompleted(tableClient)))
        {
            _logger.LogInformation("Table storage {Partition} {RowKey} did not contain all parts in table {Name}. Returning from the function.", LatestCompaniesHouseData, CurrentIngestionFileParts, tableClient.Name);
            return 0;
        }

        var currentPartitionValue = await GetPartitionRowValue(tableClient, LatestCompaniesHouseData, CurrentIngestion);
        if (currentPartitionValue is null)
        {
            _logger.LogInformation("Table storage {Partition} {RowKey} not found in table {Name}. Returning from the function.", LatestCompaniesHouseData, CurrentIngestion, tableClient.Name);
            return 0;
        }

        var toDeletePartitionValue = await GetPartitionRowValue(tableClient, LatestCompaniesHouseData, ToDelete);
        var deletedRecordsCount = await DeleteOldData(tableClient, toDeletePartitionValue);

        var latestPartitionValue = await GetPartitionRowValue(tableClient, LatestCompaniesHouseData, Latest);
        var previousPartitionValue = await GetPartitionRowValue(tableClient, LatestCompaniesHouseData, Previous);

        if (latestPartitionValue != currentPartitionValue)
        {
            if (previousPartitionValue is not null && previousPartitionValue != toDeletePartitionValue)
            {
                _logger.LogInformation("Table storage setting {Partition} {RowKey} to {Value} in table {Name}.", LatestCompaniesHouseData, ToDelete, previousPartitionValue, tableClient.Name);
                await UpsertPartitionRowValue(tableClient, LatestCompaniesHouseData, ToDelete, previousPartitionValue);
            }

            if (latestPartitionValue is not null && latestPartitionValue != previousPartitionValue)
            {
                _logger.LogInformation("Table storage setting {Partition} {RowKey} to {Value} in table {Name}.", LatestCompaniesHouseData, ToDelete, previousPartitionValue, tableClient.Name);
                await UpsertPartitionRowValue(tableClient, LatestCompaniesHouseData, Previous, latestPartitionValue);
            }

            _logger.LogInformation("Table storage setting {Partition} {RowKey} to {Value} in table {Name}.", LatestCompaniesHouseData, ToDelete, previousPartitionValue, tableClient.Name);
            await UpsertPartitionRowValue(tableClient, LatestCompaniesHouseData, Latest, currentPartitionValue);
        }

        _logger.LogInformation("Table storage deleting partition {Partition} from table {Name}.", CurrentIngestionFileParts, tableClient.Name);
        await DeleteByPartitionKey(tableClient.Name, CurrentIngestionFileParts);

        await tableClient.DeleteEntityAsync(new CompanyHouseTableEntity
        {
            PartitionKey = LatestCompaniesHouseData,
            RowKey = CurrentIngestion
        });

        return deletedRecordsCount;
    }

    private async Task<int> DeleteOldData(TableClient tableClient, string? toDeletePartitionValue)
    {
        if (toDeletePartitionValue is null)
        {
            return 0;
        }

        var deletedRecordsCount = await DeleteByPartitionKey(tableClient.Name, toDeletePartitionValue);

        await tableClient.DeleteEntityAsync(new CompanyHouseTableEntity
        {
            PartitionKey = LatestCompaniesHouseData,
            RowKey = ToDelete
        });

        _logger.LogInformation("Table storage deleted {DeletedCount} records from storage table {Name}.", deletedRecordsCount, tableClient.Name);

        return deletedRecordsCount;
    }
}