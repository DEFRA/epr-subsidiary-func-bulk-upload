using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;
/* using Microsoft.WindowsAzure.Storage.Table; */

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class DownloadStatusStorage(TableServiceClient tableServiceClient, TimeProvider timeProvider, ILogger<DownloadStatusStorage> logger)
    : IDownloadStatusStorage
{
    public const string CompaniesHouseDownloadTableName = "CompaniesHouseDownload";
    public const string PartialFilename = "BasicCompanyData";
    public const int InitialExpectedFileCountSeed = 7; // This is the number of files whilst developing

    private readonly TableServiceClient tableServiceClient = tableServiceClient;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger<DownloadStatusStorage> logger = logger;

    public async Task<bool> GetCompaniesHouseFileDownloadStatusAsync(string partitionKey)
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        var now = timeProvider.GetUtcNow();

        try
        {
            await tableClient.CreateIfNotExistsAsync();
            var downloadProgressList = await GetCompaniesHouseDownloadsList(tableClient, partitionKey);

            if (downloadProgressList == null || downloadProgressList.Count <= 0)
            {
                return true;
            }

            return downloadProgressList.Where(x => x.DownloadStatus != FileDownloadResponseCode.Succeeded).Any();
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return false;
    }

    public async Task<List<CompaniesHouseFileSetDownloadStatus>> GetCompaniesHouseFileDownloadListAsync(string partitionKey)
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        var now = timeProvider.GetUtcNow();

        try
        {
            var downloadProgressList = await GetCompaniesHouseDownloadsList(tableClient, partitionKey);

            return downloadProgressList.Where(x => x.DownloadStatus != FileDownloadResponseCode.Succeeded).ToList();
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return new List<CompaniesHouseFileSetDownloadStatus>();
    }

    public async Task<bool> SetCompaniesHouseFileDownloadStatusAsync(CompaniesHouseFileSetDownloadStatus status)
    {
        var success = false;
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);

        try
        {
            await tableClient.UpsertEntityAsync(status, TableUpdateMode.Merge);
            success = true;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return success;
    }

    public async Task CreateCompaniesHouseFileDownloadLogAsync(string partitionKey)
    {
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        await tableClient.CreateIfNotExistsAsync();

        var downloadsLog = await GetCompaniesHouseDownloadsList(tableClient, partitionKey);

        if (downloadsLog.Count == 0)
        {
            try
            {
                var now = timeProvider.GetUtcNow();
                var entities = new List<CompaniesHouseFileSetDownloadStatus>();

                for (int i = 1; i <= InitialExpectedFileCountSeed; i++)
                {
                    var fileName = $"{PartialFilename}-{now.Year}-{now.Month.ToString("00")}-01-part{i}_{InitialExpectedFileCountSeed}.zip";
                    entities.Add(new CompaniesHouseFileSetDownloadStatus
                    {
                        PartitionKey = partitionKey,
                        RowKey = RowKeyForThisMonth(now, i),
                        DownloadedFileName = fileName,
                        DownloadStatus = null
                    });
                }

                var batchTransactions = new List<TableTransactionAction>();
                batchTransactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)));
                await tableClient.SubmitTransactionAsync(batchTransactions);
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
            }
        }
    }

    private static string RowKeyForThisMonth(DateTimeOffset now, int filePart) => RowKeyForMonth(now, filePart);

    private static string RowKeyForLastMonth(DateTimeOffset now, int filePart) => RowKeyForMonth(now.AddMonths(-1), filePart);

    private static string RowKeyForMonth(DateTimeOffset when, int filePart) => $"Part-{filePart}-{when.Month}-{when.Year}";

    private async Task<List<CompaniesHouseFileSetDownloadStatus>> GetCompaniesHouseDownloadsList(TableClient tableClient, string partitionKey)
    {
        return await tableClient.QueryAsync<CompaniesHouseFileSetDownloadStatus>(x => x.PartitionKey == partitionKey).ToListAsync();
    }
}
