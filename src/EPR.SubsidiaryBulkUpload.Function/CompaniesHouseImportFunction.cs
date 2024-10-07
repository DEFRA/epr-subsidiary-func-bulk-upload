using System.Globalization;
using System.IO.Compression;
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

public class CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger, ICsvProcessor csvProcessor, ITableStorageProcessor tableStorageProcessor, IOptions<TableStorageOptions> tableStorageOptions)
{
    private readonly ICsvProcessor _csvProcessor = csvProcessor;
    private readonly ITableStorageProcessor _tableStorageProcessor = tableStorageProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger = logger;
    private readonly TableStorageOptions _tableStorageOptions = tableStorageOptions.Value;

    [Function(nameof(CompaniesHouseImportFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:CompaniesHouseContainerName%", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        var downloadStreamingResult = await client.DownloadStreamingAsync();
        var metadata = downloadStreamingResult.Value.Details?.Metadata;

        var fileName = metadata.GetFileName() ?? client.Name;

        var partitionKey = fileName.ToFindPartitionKey();

        if (!string.IsNullOrEmpty(partitionKey))
        {
            var content = downloadStreamingResult.Value.Content;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            IEnumerable<CompanyHouseTableEntity> records = null;
            if (string.Compare(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase) == 0)
            {
                using (var zipArchive = new ZipArchive(content, ZipArchiveMode.Read))
                {
                    if(zipArchive.Entries.Count > 0)
                    {
                        using (var entryStream = zipArchive.Entries[0].Open())
                        {
                            records = await _csvProcessor.ProcessStream<CompanyHouseTableEntity>(entryStream, config);
                        }
                    }
                }
            }
            else
            {
                records = await _csvProcessor.ProcessStream<CompanyHouseTableEntity>(content, config);
            }

            if (records is not null && records.Any())
            {
                await _tableStorageProcessor.WriteToAzureTableStorage(records, _tableStorageOptions.CompaniesHouseOfflineDataTableName, partitionKey);
            }

            _logger.LogInformation("CompaniesHouseImport blob trigger processed {Count} records from csv blob {Name}", records?.Count() ?? 0, client.Name);
        }
        else
        {
            _logger.LogInformation("CompaniesHouseImport blob trigger function did not process file because name '{Name}' doesn't contain partition key", fileName);
        }

        var isDeleted = await client.DeleteIfExistsAsync();
        if (isDeleted?.Value == true)
        {
            _logger.LogInformation("Blob {Name} was deleted.", client.Name);
        }
    }
}
