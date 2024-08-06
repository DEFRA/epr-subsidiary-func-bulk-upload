using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private readonly ILogger<CompaniesHouseImportFunction> _logger;

    public CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(CompaniesHouseImportFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:CompaniesHouseContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        Stream stream,
        string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name} \n Data: {Content}", name, content);

        ////var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse("UseDevelopmentStorage=true");
        ////var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        ////var table = tableClient.GetTableReference("MyTable");

        ////table.CreateIfNotExists();

        string csvFilePath = "C:/Users/a927252/OneDrive - Eviden/Desktop/Test_Blob.csv";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            ////MissingFieldFound = null,
            ////BadDataFound = null
        };

        try
        {
            // using (var reader = new StreamReader(csvFilePath))
            //// using (var blobStreamReader = new StreamReader(stream))
            using (var reader = new StringReader(content))
            using (var csv = new CsvReader(reader, config))
            {
                var records = new List<MyEntity>(csv.GetRecords<MyEntity>());

                foreach (var record in records)
                {
                    // Example update: Append "_Updated" to Property1
                    record.PartitionKey = "YourPartitionKey_Updated";
                    record.RowKey = Guid.NewGuid().ToString();
                }

                // Connect to the local Azure Table Storage
                var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
                var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference("MyTable");

                await table.CreateIfNotExistsAsync();

                // Insert records into the table
                foreach (var record in records)
                {
                    // Set partition key and row key as needed
                    ////record.PartitionKey = "YourPartitionKey"; // Customize as per your needs
                    ////record.RowKey = Guid.NewGuid().ToString(); // Customize as per your needs

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
