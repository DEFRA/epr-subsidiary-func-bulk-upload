/*using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;*/

using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class BulkUploadFunction
{
    private readonly ILogger<BulkUploadFunction> _logger;
    private readonly ISubsidiaryService _organisationService;
    private readonly ICompaniesHouseLookupService _companiesHouseLookupService;
    private readonly ICsvProcessor _csvProcessor;

    public BulkUploadFunction(ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService, ILogger<BulkUploadFunction> logger, ICsvProcessor csvProcessor)
    {
        _organisationService = organisationService;
        _companiesHouseLookupService = companiesHouseLookupService;
        _logger = logger;
        _csvProcessor = csvProcessor;
    }

    [Function(nameof(BulkUploadFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:SubsidiaryContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        var downloadStreamingResult = await client.DownloadStreamingAsync();
        var content = downloadStreamingResult.Value.Content;

        if (Path.GetExtension(client.Name) == ".csv")
        {
            var recordsProcessed = await _csvProcessor.ProcessStream(content, _organisationService, _companiesHouseLookupService);
            _logger.LogInformation("Blob trigger processed {Count} records from Client {Name}", recordsProcessed, client.Name);
        }
        else
        {
            _logger.LogInformation("Blob trigger function did not processed non-csv Client {Name}", client.Name);
        }

        _logger.LogInformation("Client process completed : {Name}", client.Name);
    }
}
