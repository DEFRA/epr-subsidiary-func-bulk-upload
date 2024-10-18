using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
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
                retryAttempt =>
                {
                    var waitTime = Math.Pow(apiOptions.RetryPolicyInitialWaitTime, retryAttempt);
                    return waitTime.ToTimespan(apiOptions.TimeUnits);
                },
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
        var options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        return TimeoutPolicy(options.Timeout, options.TimeUnits);
        /*
        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: options.Timeout.ToTimespan(options.TimeUnits),
                timeoutStrategy: TimeoutStrategy.Optimistic);
        */
    }

    public static AsyncRetryPolicy<HttpResponseMessage> AntivirusRetryPolicy<T>(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
                options.RetryPolicyMaxRetries,
                retryAttempt =>
                {
                    var waitTime = Math.Pow(options.RetryPolicyInitialWaitTime, retryAttempt);
                    return waitTime.ToTimespan(options.TimeUnits);
                },
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

    public static AsyncTimeoutPolicy<HttpResponseMessage> AntivirusTimeoutPolicy(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

        return TimeoutPolicy(options.Timeout, options.TimeUnits);
        /*
        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: antivirusApiOptions.Timeout.ToTimespan(antivirusApiOptions.TimeUnits),
                timeoutStrategy: TimeoutStrategy.Optimistic);
        */
    }

    public static AsyncRetryPolicy<HttpResponseMessage> CompaniesHouseDownloadRetryPolicy<T>(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<CompaniesHouseDownloadOptions>>().Value;

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                options.RetryPolicyMaxRetries,
                retryAttempt =>
                {
                    var waitTime = Math.Pow(options.RetryPolicyInitialWaitTime, retryAttempt);
                    return waitTime.ToTimespan(options.TimeUnits);
                },
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

    private static AsyncRetryPolicy<HttpResponseMessage> RetryPolicy<T>(int initialWaitTime, TimeUnit timeUnits, int maxRetries, IServiceProvider sp) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt =>
                {
                    var waitTime = Math.Pow(initialWaitTime, retryAttempt);
                    return waitTime.ToTimespan(timeUnits);
                },
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

    private static AsyncTimeoutPolicy<HttpResponseMessage> TimeoutPolicy(int timeout, TimeUnit timeUnits) =>
        Policy
        .TimeoutAsync<HttpResponseMessage>(
            timeout: timeout.ToTimespan(timeUnits),
            timeoutStrategy: TimeoutStrategy.Optimistic);
}
