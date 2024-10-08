using Azure;
using Azure.Data.Tables;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

public class CompaniesHouseFileSetDownloadStatus : ITableEntity
{
    public int? CurrentRunExpectedFileCount { get; set; }

    public string DownloadedFileName { get; set; }

    public FileDownloadResponseCode? DownloadStatus { get; set; }

    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
