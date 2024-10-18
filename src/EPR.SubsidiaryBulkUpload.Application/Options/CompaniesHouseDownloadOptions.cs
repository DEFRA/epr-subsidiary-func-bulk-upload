using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class CompaniesHouseDownloadOptions
{
    public const string SectionName = "CompaniesHouseDownload";

    public string CompaniesHouseDataDownloadUrl { get; set; } = null!;

    public int RetryPolicyInitialWaitTime { get; set; }

    public int RetryPolicyMaxRetries { get; set; }

    public TimeUnit TimeUnits { get; set; } = TimeUnit.Seconds;
}