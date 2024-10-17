using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class DownloadStatusStorage(TableServiceClient tableServiceClient, TimeProvider timeProvider, ILogger<DownloadStatusStorage> logger)
    : IDownloadStatusStorage
{
    public const string CompaniesHouseDownloadTableName = "CompaniesHouseDownload";
    public const string PartialFilename = "BasicCompanyData";
    public const int InitialExpectedFileCountSeed = 7;

    private readonly TableServiceClient _tableServiceClient = tableServiceClient;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DownloadStatusStorage> _logger = logger;

    public async Task<bool> GetCompaniesHouseFileDownloadStatusAsync(string partitionKey)
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        var tableClient = _tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        var now = _timeProvider.GetUtcNow();

        try
        {
            await tableClient.CreateIfNotExistsAsync();
            var downloadProgressList = await tableClient.QueryAsync<CompaniesHouseFileSetDownloadStatus>(x => x.PartitionKey == partitionKey).ToListAsync();

            if (downloadProgressList == null || downloadProgressList.Count <= 0)
            {
                return true;
            }

            return downloadProgressList.Where(x => x.DownloadStatus != FileDownloadResponseCode.Succeeded).Any();
        }
        catch (RequestFailedException ex)
            {
            _logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return false;
    }

    public async Task<List<CompaniesHouseFileSetDownloadStatus>> GetCompaniesHouseFileDownloadListAsync(string partitionKey)
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        var tableClient = _tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        var now = _timeProvider.GetUtcNow();

        try
        {
            var downloadProgressList = await tableClient.QueryAsync<CompaniesHouseFileSetDownloadStatus>(x => x.PartitionKey == partitionKey).ToListAsync();

            return downloadProgressList.Where(x => x.DownloadStatus != FileDownloadResponseCode.Succeeded).ToList();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return new List<CompaniesHouseFileSetDownloadStatus>();
    }

    public async Task<bool> SetCompaniesHouseFileDownloadStatusAsync(CompaniesHouseFileSetDownloadStatus status)
    {
        var success = false;
        var tableClient = _tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);

        try
        {
            await tableClient.UpsertEntityAsync(status, TableUpdateMode.Merge);
            success = true;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return success;
    }

    public async Task CreateCompaniesHouseFileDownloadLogAsync(string partitionKey)
    {
        var tableClient = _tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        await tableClient.CreateIfNotExistsAsync();
        var downloadsLog = await tableClient.QueryAsync<CompaniesHouseFileSetDownloadStatus>(x => x.PartitionKey == partitionKey).ToListAsync();

        if (downloadsLog.Count == 0)
        {
            try
            {
                var now = _timeProvider.GetUtcNow();
                var entities = new List<CompaniesHouseFileSetDownloadStatus>();

                for (int i = 1; i <= InitialExpectedFileCountSeed; i++)
                {
                    var fileName = $"{PartialFilename}-{now.Year}-{now.Month:00}-01-part{i}_{InitialExpectedFileCountSeed}.zip";

                    entities.Add(new CompaniesHouseFileSetDownloadStatus
                    {
                        PartitionKey = partitionKey,
                        RowKey = RowKeyForThisMonth(now, i),
                        DownloadFileName = fileName,
                        DownloadStatus = null
                    });
                }

                var batchTransactions = new List<TableTransactionAction>();
                batchTransactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)));
                await tableClient.SubmitTransactionAsync(batchTransactions);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);

                throw;
            }
        }
    }

    private static string RowKeyForThisMonth(DateTimeOffset now, int filePart) => RowKeyForMonth(now, filePart);

    private static string RowKeyForMonth(DateTimeOffset when, int filePart) => $"Part-{filePart}-{when.Month}-{when.Year}";
}
