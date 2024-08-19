using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
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
    private readonly IBulkUploadOrchestration orchestration;

    public BulkUploadFunction(ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService, ILogger<BulkUploadFunction> logger, ICsvProcessor csvProcessor, IBulkUploadOrchestration orchestration)
    {
        _organisationService = organisationService;
        _companiesHouseLookupService = companiesHouseLookupService;
        _logger = logger;
        _csvProcessor = csvProcessor;
        this.orchestration = orchestration;
    }

    [Function(nameof(BulkUploadFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:SubsidiaryContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        var downloadStreamingResult = await client.DownloadStreamingAsync();
        var metaData = downloadStreamingResult.Value.Details.Metadata;

        var content = downloadStreamingResult.Value.Content;

        if (Path.GetExtension(client.Name) == ".csv")
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            var records = await _csvProcessor.ProcessStream<CompaniesHouseCompany, CompaniesHouseCompanyMap>(content, configuration);
            await orchestration.Orchestrate(records);

            _logger.LogInformation("Blob trigger processed {Count} records from Client {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("Blob trigger function did not processed non-csv Client {Name}", client.Name);
        }

        _logger.LogInformation("Client process completed : {Name}", client.Name);
    }
}
