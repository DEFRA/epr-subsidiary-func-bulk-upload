using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;

namespace EPR.SubsidiaryBulkUpload.Application.Resilience;

public static class Policies
{
    public static AsyncRetryPolicy<HttpResponseMessage> DefaultRetryPolicy<T>(IServiceProvider sp)
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

    public static AsyncTimeoutPolicy<HttpResponseMessage> DefaultTimeoutPolicy(IServiceProvider sp)
    {
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: apiOptions.ConvertToTimespan(apiOptions.Timeout),
                timeoutStrategy: TimeoutStrategy.Optimistic);
    }

    public static AsyncTimeoutPolicy<HttpResponseMessage> AntivirusTimeoutPolicy(IServiceProvider sp)
    {
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
        var antivirusApiOptions = sp.GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: apiOptions.ConvertToTimespan(antivirusApiOptions.Timeout),
                timeoutStrategy: TimeoutStrategy.Optimistic);
    }
}
