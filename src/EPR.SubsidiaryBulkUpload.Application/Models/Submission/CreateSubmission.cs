namespace EPR.SubsidiaryBulkUpload.Application.Models.Submission;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CreateSubmission
{
    public Guid Id { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public Guid? ComplianceSchemeId { get; set; }
}