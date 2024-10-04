using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class SubmissionApiOptions
{
    public const string SectionName = "SubmissionApi";

    public string BaseUrl { get; set; }
}