using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class BulkUploadFunction
{
    private readonly ILogger<BulkUploadFunction> _logger;
    private readonly ICsvProcessor _csvProcessor;
    private readonly IBulkUploadOrchestration _orchestration;

    public BulkUploadFunction(ILogger<BulkUploadFunction> logger, ICsvProcessor csvProcessor, IBulkUploadOrchestration orchestration)
    {
        _logger = logger;
        _csvProcessor = csvProcessor;
        _orchestration = orchestration;
    }

    [Function(nameof(BulkUploadFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:SubsidiaryContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        BlobClient client)
    {
        var downloadStreamingResult = await client.DownloadStreamingAsync();
        var metaData = downloadStreamingResult.Value.Details?.Metadata;

        var userGuid = metaData.Where(pair => pair.Key.Contains("userId"))
                        .Select(pair => pair.Value).FirstOrDefault();

        var hasUserId = Guid.TryParse(userGuid, out var userId);
        if (!hasUserId)
        {
            _logger.LogWarning("Missing userId metadata for blob {Name}", client.Name);
        }

        var content = downloadStreamingResult.Value.Content;

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        var userRequestModel = metaData.ToUserRequestModel();

        if (userRequestModel != null)
        {
            var records = await _csvProcessor.ProcessStream<CompaniesHouseCompany, CompaniesHouseCompanyMap>(content, configuration);
            await _orchestration.Orchestrate(records, userId, userRequestModel);

            _logger.LogInformation("Blob trigger processed {Count} records from csv blob {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("Blob trigger stopped, Missing userId or organisationId in the metadata for blob {Name}", client.Name);
        }
    }
}
