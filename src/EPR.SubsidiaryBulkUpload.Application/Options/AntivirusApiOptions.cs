using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class AntivirusApiOptions
{
    public const string SectionName = "AntivirusApi";

    public string BaseUrl { get; set; }

    public string SubscriptionKey { get; set; }

    public string TenantId { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Scope { get; set; }

    public int Timeout { get; set; }

    public TimeUnit TimeUnits { get; set; } = TimeUnit.Seconds;

    public int RetryPolicyInitialWaitTime { get; set; }

    public int RetryPolicyMaxRetries { get; set; }

    public string CollectionSuffix { get; set; }

    public string NotificationEmail { get; set; }
}