using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseFilePostService(
                ISubmissionStatusClient submissionStatusClient,
                IAntivirusClient antivirusClient,
                IOptions<AntivirusApiOptions> antiVirusOptions,
                IOptions<BlobStorageOptions> blobOptions,
                IOptions<ApiOptions> apiOptions) : ICompaniesHouseFilePostService
{
    public const string SubmissionPeriodText = "NA Companies house data File Upload";
    private readonly ISubmissionStatusClient submissionStatusClient = submissionStatusClient;
    private readonly IAntivirusClient antivirusClient = antivirusClient;
    private readonly AntivirusApiOptions antiVirusOptions = antiVirusOptions.Value;
    private readonly BlobStorageOptions blobOptions = blobOptions.Value;
    private readonly ApiOptions apiOptions = apiOptions.Value;

    public async Task<HttpStatusCode> PostFileAsync(Stream stream, string fileName)
    {
        var fileId = Guid.NewGuid();
        if(!Guid.TryParse(apiOptions.SystemUserId, out Guid systemUserId))
        {
            return HttpStatusCode.BadRequest;
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
            UserId = systemUserId,
            UserEmail = "system@dummy.com"
        };

        var statusCode = await submissionStatusClient.CreateSubmissionAsync(submission)
            .ThenIfIsSuccessStatusCode(() => submissionStatusClient.CreateEventAsync(antiVirusEvent, fileId))
            .ThenIfIsSuccessStatusCode(() => antivirusClient.SendFileAsync(fileDetails, fileName, stream));

        return statusCode;
    }
}
