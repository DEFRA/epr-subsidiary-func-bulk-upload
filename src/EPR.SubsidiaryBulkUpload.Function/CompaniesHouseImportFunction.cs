using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.Services.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Services.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private const int BatchSize = 100; // Maximum batch size for Azure Table Storage

    private readonly ICsvProcessor _csvProcessor;
    private readonly ITableStorageProcessor _tableStorageProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger;
    private readonly ConfigOptions _configOptions;

    public CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger, ICsvProcessor csvProcessor, ITableStorageProcessor tableStorageProcessor, IOptions<ConfigOptions> configOptions)
    {
        _logger = logger;
        _csvProcessor = csvProcessor;
        _tableStorageProcessor = tableStorageProcessor;
        _configOptions = configOptions.Value;
    }

    [Function(nameof(CompaniesHouseImportFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:CompaniesHouseContainerName%", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        var downloadStreamingResult = await client.DownloadStreamingAsync();
        var metadata = downloadStreamingResult.Value.Details?.Metadata;

        if (metadata is not null && metadata.Count > 0)
        {
            foreach (var metadataItem in metadata)
            {
                _logger.LogInformation("Blob {Name} has metadata {Key} {Value}", client.Name, metadataItem.Key, metadataItem.Value);
            }
        }

        var partitionKey = client.Name.ToFindPartitionKey();

        if (!string.IsNullOrEmpty(partitionKey))
        {
            var content = downloadStreamingResult.Value.Content;

            var storageConnectionString = _configOptions.TableStorageConnectionString;
            var tableName = _configOptions.CompaniesHouseOfflineDataTableName;

            var records = await _csvProcessor.ProcessStreamToObject(content, new CompanyHouseTableEntity());

            if (records.Any())
            {
                await _tableStorageProcessor.WriteToAzureTableStorage(records, tableName, partitionKey, storageConnectionString, BatchSize);
            }

            _logger.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("C# Blob trigger function did not processed file name doesn't contain partition key {Name}", client.Name);
        }
    }
}
