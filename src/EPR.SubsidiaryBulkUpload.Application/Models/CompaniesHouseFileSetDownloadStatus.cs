using Azure;
using Azure.Data.Tables;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

public class CompaniesHouseFileSetDownloadStatus : ITableEntity
{
    public int LastRunFileCount { get; set; }

    public int? CurrentRunExpectedFileCount { get; set; }

    public string DownlaodedFileNamesCsv { get; set; }

    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
