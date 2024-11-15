using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;

namespace EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;

public interface ISubmissionStatusClient
{
    Task<HttpStatusCode> CreateSubmissionAsync(CreateSubmission submission);

    Task<HttpStatusCode> CreateEventAsync(AntivirusCheckEvent antivirusEvent, Guid submissionId);
}