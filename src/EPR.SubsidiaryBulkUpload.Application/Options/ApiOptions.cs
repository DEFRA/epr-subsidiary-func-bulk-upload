using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class ApiOptions
{
    public const string SectionName = "ApiConfig";

    public string CompaniesHouseLookupBaseUrl { get; set; } = null!;

    public string CompaniesHouseDirectBaseUri { get; set; }

    public bool UseDirectCompaniesHouseLookup { get; set; } = false;

    public string CompaniesHouseDirectApiKey { get; set; }

    public string AccountServiceClientId { get; set; } = null!;

    public string Certificate { get; set; } = null!;

    public int Timeout { get; set; }

    public TimeUnit TimeUnits { get; set; } = TimeUnit.Seconds;

    public string SubsidiaryServiceBaseUrl { get; set; } = null!;

    public int RetryPolicyInitialWaitTime { get; set; }

    public int RetryPolicyMaxRetries { get; set; }

    public int RetryPolicyTooManyAttemptsWaitTime { get; set; }

    public int RetryPolicyTooManyAttemptsMaxRetries { get; set; }
}
