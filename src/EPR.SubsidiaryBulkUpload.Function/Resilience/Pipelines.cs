using System.Diagnostics.CodeAnalysis;
using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace EPR.SubsidiaryBulkUpload.Function.Resilience;

[ExcludeFromCodeCoverage]
public static class Pipelines
{
    public const string CompaniesHouseResiliencePipelineKey = "CompaniesHouseResiliencePipeline";

    public static Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> ConfigureCompaniesHouseResilienceHandler<T>()
    {
        return (builder, context) =>
        {
            var apiOptions = context.GetOptions<ApiOptions>();

            builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(apiOptions.RetryPolicyInitialWaitTime),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => response.StatusCode != HttpStatusCode.TooManyRequests),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = apiOptions.RetryPolicyMaxRetries,
                UseJitter = true,
                OnRetry = args =>
                {
                    var logger = context.ServiceProvider.GetService<ILogger<T>>();

                    if (args.Outcome.Exception is TimeoutRejectedException)
                    {
                        logger?.LogInformation("Timeout encountered");
                    }

                    logger?.LogWarning(
                            "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. {ExceptionMessage}",
                            typeof(T).Name,
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message);

                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.TooManyRequests),
                Delay = TimeSpan.FromSeconds(apiOptions.RetryPolicyTooManyAttemptsWaitTime),
                MaxRetryAttempts = apiOptions.RetryPolicyMaxRetries,
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    context.ServiceProvider.GetService<ILogger<T>>()?
                        .LogWarning(
                            "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a 429 error. {ExceptionMessage}",
                            typeof(T).Name,
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message);

                    return default;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(apiOptions.Timeout)
            });
        };
    }
}
