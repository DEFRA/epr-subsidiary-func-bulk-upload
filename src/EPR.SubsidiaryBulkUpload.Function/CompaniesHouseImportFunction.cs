using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseImportFunction
{
    private readonly IAntivirusApiClient _antivirusApiClient;
    private readonly IConfiguration _configuration;
    private readonly ICsvProcessor _csvProcessor;
    private readonly ILogger<CompaniesHouseImportFunction> _logger;

    public CompaniesHouseImportFunction(
        IAntivirusApiClient antivirusApiClient,
        IConfiguration configuration,
        ICsvProcessor csvProcessor,
        ILogger<CompaniesHouseImportFunction> logger)
    {
        _antivirusApiClient = antivirusApiClient;
        _csvProcessor = csvProcessor;
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(CompaniesHouseImportFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:CompaniesHouseContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        /*
         * https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cextensionv5&pivots=programming-language-csharp
         *
         * https://stackoverflow.com/questions/30203070/azure-blob-downloadtostream-takes-too-long
         * https://github.com/Azure/azure-sdk-for-java/issues/35477
         *
         * https://dontpaniclabs.com/blog/post/2019/04/18/memorystream-limits-handling-large-files-in-azure-with-blob-storage-streaming/
         * https://ticehurst.com/2022/01/30/blob-streaming.html
         */
        var containerName = client.BlobContainerName;
        var name = client.Name;
        var properties = await client.GetPropertiesAsync();

        // var downloadResult = await client.DownloadContentAsync();
        // var downloadResponse = await client.DownloadToAsync(stream);
        var downloadStream = await client.DownloadStreamingAsync();

        var processCsv = _configuration.GetValue<bool?>("ProcessOptions:ProcessAsCsv");
        if (processCsv is true && Path.GetExtension(name) == ".csv")
        {
            var recordsProcessed = await _csvProcessor.ProcessStream(downloadStream.Value.Content);
            _logger.LogInformation("C# Blob trigger processed {Count} records from csv file}", name);
            return;
        }

        // downloadStream.Value.Details.  metadata, etc

        // Next few lines would be in an AntiVirusClient that calls the api
        var fileId = Guid.NewGuid(); // this should be from the submission event
        var fileName = name;

        // var userId = Guid.Parse(_configuration["Notifications:AntivirusUserId"]);
        var userId = Guid.Empty;
        var userEmail = _configuration["Notifications:AntivirusEmail"];

        var fileDetails = new FileDetails
        {
            Key = fileId,
            Extension = Path.GetExtension(fileName),
            FileName = Path.GetFileNameWithoutExtension(fileName),
            Collection = GetCollectionName("registration"), // submissionType.GetDisplayName()), // try pom first - will this cause a problem with topic supscription?
            UserId = null, // userId,
            UserEmail = userEmail
        };

        if (downloadStream.Value.Content.Position != 0)
        {
            downloadStream.Value.Content.Seek(0, SeekOrigin.Begin);
        }

        await _antivirusApiClient.SendFileAsync(fileDetails, name, downloadStream.Value.Content);

        // stream.Seek(0, SeekOrigin.Begin);
        /*
         mock - https://stackoverflow.com/questions/70632556/mock-blobclient-for-azure-function
         */
        /*
         https://stackoverflow.com/questions/64388123/how-to-stream-binary-file-from-the-blob-storage-through-azure-function

        byte[] certData;

       if (await blobClient.ExistsAsync())
       {
           var memorystream = new MemoryStream();
           blobClient.DownloadTo(memorystream);

           certData = memorystream.ToArray();

           X509Certificate2 cert = new X509Certificate2(certData, password);

           log.LogInformation("Found the certificate on cloud storage.");
           return cert;
       }*/

        /*
         * TODO:
         *         services.AddApplicationInsightsTelemetry();

                    services.AddHttpClient<ITradeAntivirusApiClient, TradeAntivirusApiClient>(client =>
            {
                client.BaseAddress = new Uri($"{tradeAntivirusOptions.BaseUrl}/v1/");
                client.Timeout = TimeSpan.FromSeconds(tradeAntivirusOptions.Timeout);
                client.DefaultRequestHeaders.Add("OCP-APIM-Subscription-Key", tradeAntivirusOptions.SubscriptionKey);
            }).AddHttpMessageHandler<TradeAntivirusApiAuthorizationHandler>();

        C:\dev\epr_pom_api_web\WebApiGateway\WebApiGateway.Api\Clients\AntivirusClient.cs

         */

        /*
        using var blobStreamReader = new StreamReader(v.Content);
        var content = await blobStreamReader.ReadToEndAsync();

        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name} \n Data length: {Content}", name, content.Length);
        */
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name}", name);
    }

    // This should also be in the antivirus service
    private string GetCollectionName(string submissionType)
    {
        var suffix = _configuration["AntivirusApi:CollectionSuffix"]; // _antivirusOptions?.CollectionSuffix;
        return suffix is null ? submissionType : submissionType + suffix;
    }
}
