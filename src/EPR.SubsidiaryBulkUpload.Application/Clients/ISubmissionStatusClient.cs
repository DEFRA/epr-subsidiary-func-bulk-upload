﻿using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;

namespace EPR.SubsidiaryBulkUpload.Application.Clients;

public interface ISubmissionStatusClient
{
    Task<HttpStatusCode> CreateSubmissionAsync(CreateSubmission submission);

    Task<HttpStatusCode> CreateEventAsync<T>(T @event, Guid submissionId, Guid? userId = null, Guid? organisationId = null)
        where T : AbstractEvent;
}