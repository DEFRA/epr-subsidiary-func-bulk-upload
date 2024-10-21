using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class ApiOptions
{
    public const string SectionName = "ApiConfig";

    public string CompaniesHouseLookupBaseUrl { get; set; } = null!;

    public string CompaniesHouseDirectBaseUri { get; set; }

    public string CompaniesHouseDirectApiKey { get; set; }

    public string CompaniesHouseDataDownloadUrl { get; set; } = null!;

    public string CompaniesHouseFileDownloadPath { get; set; } = null!;

    public string AccountServiceClientId { get; set; } = null!;

    public string Certificate { get; set; } = null!;

    public int Timeout { get; set; }

    public TimeUnit TimeUnits { get; set; } = TimeUnit.Seconds;

    public string SubsidiaryServiceBaseUrl { get; set; } = null!;

    public int RetryPolicyMaxRetries { get; set; }

    public int RetryPolicyInitialWaitTime { get; set; }

    public int RetryPolicyTooManyAttemptsMaxRetries { get; set; }

    public int RetryPolicyTooManyAttemptsWaitTime { get; set; }
}
