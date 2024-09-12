using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class SubmissionStatusApiOptions
{
    public const string SectionName = "SubmissionStatusApi";

    public string BaseUrl { get; set; }
}