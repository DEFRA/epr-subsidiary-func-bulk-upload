using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace EPR.SubsidiaryBulkUpload.Application.Resilience;

public static class Pipelines
{
    public const string CompaniesHouseResiliencePipelineKey = "CompaniesHouseResiliencePipeline";

    public static IHttpResiliencePipelineBuilder AddCompaniesHouseResilienceHandler(this IHttpClientBuilder builder) =>
    builder.AddResilienceHandler(CompaniesHouseResiliencePipelineKey, ConfigureCompaniesHouseResilienceHandler<CompaniesHouseLookupService>());

    public static IHttpClientBuilder AddCompaniesHouseResilienceHandlerToHttpClientBuilder(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler(CompaniesHouseResiliencePipelineKey, ConfigureCompaniesHouseResilienceHandler<CompaniesHouseLookupService>());
        return builder;
    }

    public static Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> ConfigureCompaniesHouseResilienceHandler<T>()
    {
        return (builder, context) =>
        {
            var apiOptions = context.GetOptions<ApiOptions>();

            builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                Delay = apiOptions.ConvertToTimespan(apiOptions.RetryPolicyInitialWaitTime),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => response.StatusCode != HttpStatusCode.TooManyRequests),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = apiOptions.RetryPolicyMaxRetries,
                UseJitter = true,
                OnRetry = args =>
                {
                    context.ServiceProvider.GetService<ILogger<T>>()?
                        .LogWarning(
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
                Delay = apiOptions.ConvertToTimespan(apiOptions.RetryPolicyTooManyAttemptsWaitTime),
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
                Timeout = apiOptions.ConvertToTimespan(apiOptions.Timeout)
            });
        };
    }
}
