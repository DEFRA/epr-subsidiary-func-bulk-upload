using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class DownloadStatusStorage(TableServiceClient tableServiceClient, TimeProvider timeProvider, ILogger<DownloadStatusStorage> logger)
    : IDownloadStatusStorage
{
    public const string CompaniesHouseDownloadTableName = "CompaniesHouseDownload";
    public const string CompaniesHouseDownloadPartitionKey = "CompaniesHouseDownload";
    public const string MonthPartialRowKey = "Month";
    public const int InitialExpectedFileCountSeed = 7; // This is the number of files whilst developing

    private readonly TableServiceClient tableServiceClient = tableServiceClient;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger<DownloadStatusStorage> logger = logger;

    public async Task<CompaniesHouseFileSetDownloadStatus?> GetCompaniesHouseFileDownloadStatusAsync()
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);
        var now = timeProvider.GetUtcNow();

        try
        {
            await tableClient.CreateIfNotExistsAsync();

            var rowKey = RowKeyForThisMonth(now);
            result = await GetStatus(tableClient, rowKey);

            if (result == null)
            {
                rowKey = RowKeyForLastMonth(now);

                result = await GetStatus(tableClient, rowKey);

                result = new CompaniesHouseFileSetDownloadStatus
                {
                    CurrentRunExpectedFileCount = result?.CurrentRunExpectedFileCount ?? InitialExpectedFileCountSeed
                };
            }
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return result;
    }

    public async Task<bool> SetCompaniesHouseFileDownloadStatusAsync(CompaniesHouseFileSetDownloadStatus status)
    {
        var success = false;
        var tableClient = tableServiceClient.GetTableClient(CompaniesHouseDownloadTableName);

        try
        {
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.UpsertEntityAsync(status, TableUpdateMode.Merge);
            success = true;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or create table {TableName}", CompaniesHouseDownloadTableName);
        }

        return success;
    }

    private static string RowKeyForThisMonth(DateTimeOffset now) => RowKeyForMonth(now);

    private static string RowKeyForLastMonth(DateTimeOffset now) => RowKeyForMonth(now.AddMonths(-1));

    private static string RowKeyForMonth(DateTimeOffset when) => $"{MonthPartialRowKey}-{when.Month}-{when.Year}";

    private async Task<CompaniesHouseFileSetDownloadStatus?> GetStatus(TableClient tableClient, string rowKey)
    {
        CompaniesHouseFileSetDownloadStatus result = null;
        try
        {
            var response = await tableClient.GetEntityAsync<CompaniesHouseFileSetDownloadStatus>(
                CompaniesHouseDownloadPartitionKey, rowKey);
            result = response?.Value;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Cannot get or table entity from {Table} row {Row}", CompaniesHouseDownloadTableName, rowKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot get or table entity from {Table} row {Row}", CompaniesHouseDownloadTableName, rowKey);
        }

        return result;
    }
}
