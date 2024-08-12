using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.Services.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Services.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private const int BatchSize = 100; // Maximum batch size for Azure Table Storage

    private readonly ICsvProcessor _csvProcessor;
    private readonly ITableStorageProcessor _tableStorageProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger;

    public CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger, ICsvProcessor csvProcessor, ITableStorageProcessor tableStorageProcessor)
    {
        _logger = logger;
        _csvProcessor = csvProcessor;
        _tableStorageProcessor = tableStorageProcessor;
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

        if (Path.GetExtension(client.Name) == ".csv")
        {
            var partitionKey = client.Name.ToPartitionKeyFormat();

            var content = downloadStreamingResult.Value.Content;

            var storageConnectionString = Environment.GetEnvironmentVariable("BlobStorage__ConnectionString");

            var records = await _csvProcessor.ProcessStreamToObject(content, new CompanyHouseTableEntity());

            if(records.Any())
            {
                await _tableStorageProcessor.WriteToAzureTableStorage(records, "CompaniesHouseData", partitionKey, storageConnectionString, BatchSize);
            }

            _logger.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("C# Blob trigger function did not processed non-csv blob {Name}", client.Name);
        }
    }
}