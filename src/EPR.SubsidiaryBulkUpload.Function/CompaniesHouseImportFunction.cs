using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger, ICsvProcessor csvProcessor, ITableStorageProcessor tableStorageProcessor, IOptions<TableStorageOptions> configOptions)
{
    private readonly ICsvProcessor _csvProcessor = csvProcessor;
    private readonly ITableStorageProcessor _tableStorageProcessor = tableStorageProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger = logger;
    private readonly TableStorageOptions _configOptions = configOptions.Value;

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

            var tableName = _configOptions.CompaniesHouseOfflineDataTableName;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            var records = await _csvProcessor.ProcessStream<CompanyHouseTableEntity>(content, config);

            if (records.Any())
            {
                await _tableStorageProcessor.WriteToAzureTableStorage(records, tableName, partitionKey);
            }

            _logger.LogInformation("CompaniesHouseImport blob trigger processed {Count} records from csv blob {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("CompaniesHouseImport blob trigger function did not process file because name '{Name}' doesn't contain partition key", client.Name);
        }
    }
}
