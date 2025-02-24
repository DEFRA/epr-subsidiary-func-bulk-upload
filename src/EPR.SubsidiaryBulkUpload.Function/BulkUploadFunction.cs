using Azure.Storage.Blobs;
using EPR.SubsidiaryBulkUpload.Application.ClassMaps;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
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
        var content = downloadStreamingResult.Value.Content;
        if (content == null)
        {
            _logger.LogInformation("File without any data or invalid data");
            return;
        }

        var metaData = downloadStreamingResult.Value.Details?.Metadata;

        if (metaData is not null && metaData.Count > 0)
        {
            foreach (var metadataItem in metaData)
            {
                _logger.LogInformation("Blob {Name} has metadata {Key} {Value}", client.Name, metadataItem.Key, metadataItem.Value);
            }
        }

        var userRequestModel = metaData.ToUserRequestModel(client.Name, client.BlobContainerName);
        if (userRequestModel != null)
        {
            await _orchestration.NotifyStart(userRequestModel);
            var records = await _csvProcessor.ProcessStreamWithMapping<CompaniesHouseCompany, CompaniesHouseCompanyMap>(content, CsvConfigurations.BulkUploadCsvConfiguration);
            await _orchestration.NotifyErrors(records, userRequestModel);
            await _orchestration.Orchestrate(records, userRequestModel);
            _logger.LogInformation("Blob trigger processed {Count} records from csv blob {Name}", records.Count(), client.Name);
        }
        else
        {
            _logger.LogInformation("Blob trigger stopped, missing userId or organisationId in the metadata for blob {Name}", client.Name);
        }
    }
}