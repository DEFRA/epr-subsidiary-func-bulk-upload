using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseFilePostService(
                ISubmissionStatusClient submissionStatusClient,
                IAntivirusClient antivirusClient,
                ISystemDetailsProvider systemDetailsProvider,
                ILogger<CompaniesHouseFilePostService> logger,
                IOptions<AntivirusApiOptions> antiVirusOptions,
                IOptions<BlobStorageOptions> blobOptions) : ICompaniesHouseFilePostService
{
    public const string SubmissionPeriodText = "NA Companies house data File Upload";
    private readonly ISubmissionStatusClient submissionStatusClient = submissionStatusClient;
    private readonly IAntivirusClient antivirusClient = antivirusClient;
    private readonly ISystemDetailsProvider systemDetailsProvider = systemDetailsProvider;
    private readonly ILogger<CompaniesHouseFilePostService> logger = logger;

    private readonly AntivirusApiOptions antiVirusOptions = antiVirusOptions.Value;
    private readonly BlobStorageOptions blobOptions = blobOptions.Value;

    public async Task<HttpStatusCode> PostFileAsync(Stream stream, string fileName)
    {
        var fileId = Guid.NewGuid();

        var systemUserId = systemDetailsProvider.SystemUserId;
        if (systemUserId is null)
        {
            logger.LogError("System user id was not found");
            return HttpStatusCode.InternalServerError;
        }

        var submission = new CreateSubmission
        {
            Id = fileId,
            DataSourceType = DataSourceType.File,
            SubmissionType = SubmissionType.CompaniesHouse,
            SubmissionPeriod = SubmissionPeriodText,
            ComplianceSchemeId = null
        };

        var antiVirusEvent = new AntivirusCheckEvent
        {
            FileName = fileName,
            FileType = FileType.CompaniesHouse,
            FileId = fileId,
            BlobContainerName = blobOptions.CompaniesHouseContainerName,
            RegistrationSetId = null
        };

        var fileDetails = new FileDetails
        {
            Key = fileId,
            Extension = Path.GetExtension(fileName),
            FileName = Path.GetFileNameWithoutExtension(fileName),
            Collection = SubmissionType.CompaniesHouse.GetDisplayName() + (antiVirusOptions.CollectionSuffix ?? string.Empty),
            UserId = systemUserId.Value,
            UserEmail = antiVirusOptions.NotificationEmail
        };

        var statusCode = await submissionStatusClient.CreateSubmissionAsync(submission)
            .ThenIfIsSuccessStatusCode(() => submissionStatusClient.CreateEventAsync(antiVirusEvent, fileId))
            .ThenIfIsSuccessStatusCode(() => antivirusClient.SendFileAsync(fileDetails, fileName, stream));

        return statusCode;
    }
}
