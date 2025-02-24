using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UserRequestModel
{
    public string? BlobName { get; set; }

    public string? BlobContainerName { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string? FileName { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid? SubmissionId { get; set; }

    public Guid UserId { get; set; }
}
