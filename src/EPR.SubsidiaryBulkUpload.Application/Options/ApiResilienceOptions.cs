using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public abstract class ApiResilienceOptions
{
    public int RetryPolicyInitialWaitTime { get; set; }

    public int RetryPolicyMaxRetries { get; set; }

    public int RetryPolicyTooManyAttemptsWaitTime { get; set; }

    public int RetryPolicyTooManyAttemptsMaxRetries { get; set; }

    public TimeUnit TimeUnits { get; set; } = TimeUnit.Seconds;
}