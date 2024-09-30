using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace EPR.SubsidiaryBulkUpload.Application.Resilience;

public static class Policies
{
    public const string CompaniesHouseResiliencePipelineKey = "CompaniesHouseResiliencePipeline";

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<T>(IServiceProvider sp)
    {
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                apiOptions.RetryPolicyMaxRetries,
                retryAttempt => apiOptions.ConvertToTimespan(Math.Pow(apiOptions.RetryPolicyInitialWaitTime, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    sp?.GetService<ILogger<T>>()?
                        .LogWarning(
                            "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. {ExceptionMessage}",
                            typeof(T).Name,
                            retryAttempt,
                            timespan.TotalMilliseconds,
                            outcome?.Exception?.Message);
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(IServiceProvider sp)
    {
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: apiOptions.ConvertToTimespan(apiOptions.Timeout),
                timeoutStrategy: TimeoutStrategy.Optimistic);
    }
}
