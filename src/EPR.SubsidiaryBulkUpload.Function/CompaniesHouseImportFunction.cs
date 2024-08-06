using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private readonly ICsvProcessor _csvProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger;
    private string _streamContent;

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
        ////using var blobStreamReader = new StreamReader(stream);
        ////var content = await blobStreamReader.ReadToEndAsync();
        ////_logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name} \n Data: {Content}", name, content);

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
            var (streamContent, recordsProcessed) = await _csvProcessor.ProcessStreamToEnd(content);
            _streamContent = streamContent;
            _logger.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", recordsProcessed, client.Name);
        }
        else
        {
            _logger.LogInformation("C# Blob trigger function did not processed non-csv blob {Name}", client.Name);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };

        try
        {
            using (var reader = new StringReader(_streamContent))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<MyEntity>();

                // Connect to the local Azure Table Storage
                var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
                var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference("MyTable");

                await table.CreateIfNotExistsAsync();

                // Insert records into the table
                foreach (var record in records)
                {
                    // Set partition key and row key as needed
                    record.PartitionKey = "YourPartitionKey"; // Customize as per your needs
                    record.RowKey = Guid.NewGuid().ToString(); // Customize as per your needs

                    var insertOperation = TableOperation.Insert(record);
                    table.Execute(insertOperation);
                }
            }
        }
        catch (Exception ex)
        {
            var error = ex.Message;
        }
    }
}
