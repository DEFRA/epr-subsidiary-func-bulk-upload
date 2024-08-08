using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private readonly ICsvProcessor _csvProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger;

    public CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger, ICsvProcessor csvProcessor)
    {
        _logger = logger;
        _csvProcessor = csvProcessor;
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

        var content = downloadStreamingResult.Value.Content;

        if (Path.GetExtension(client.Name) == ".csv")
        {
            var recordsProcessed = await _csvProcessor.ProcessStream(content, null, null);
            _logger.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", recordsProcessed, client.Name);
        }
        else
        {
            _logger.LogInformation("C# Blob trigger function did not processed non-csv blob {Name}", client.Name);
        }
    }
}
