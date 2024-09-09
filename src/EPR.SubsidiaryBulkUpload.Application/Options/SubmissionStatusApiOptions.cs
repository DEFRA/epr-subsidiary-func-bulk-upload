using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class SubmissionStatusApiOptions
{
    public const string Section = "SubmissionStatusApi";

    public string BaseUrl { get; set; }
}