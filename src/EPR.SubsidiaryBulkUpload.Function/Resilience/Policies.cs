using System.Diagnostics.CodeAnalysis;
using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace EPR.SubsidiaryBulkUpload.Function.Resilience;

[ExcludeFromCodeCoverage]
public static class Policies
{
    public const string CompaniesHouseResiliencePipelineKey = "CompaniesHouseResiliencePipeline";

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<T>(IServiceProvider services, IConfiguration configuration) => HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        configuration.GetValue<int>("ApiConfig:RetryPolicyMaxRetries"),
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(configuration.GetValue<int>("ApiConfig:RetryPolicyInitialWaitTime"), retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            services?.GetService<ILogger<T>>()?
                .LogWarning(
                    "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. {ExceptionMessage}",
                    typeof(T).Name,
                    retryAttempt,
                    timespan.TotalMilliseconds,
                    outcome?.Exception?.Message);
        });

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(IServiceProvider sp)
    {
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(apiOptions.Timeout),
                timeoutStrategy: TimeoutStrategy.Optimistic);
    }
}
