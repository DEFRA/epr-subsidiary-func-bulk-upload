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
        builder.AddCompaniesHouseResilienceHandler();
        return builder;
    }

    public static Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> ConfigureCompaniesHouseResilienceHandler<T>()
    {
        return (builder, context) =>
        {
            var options = context.GetOptions<ApiOptions>();

            builder
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = (RetryPredicateArguments<HttpResponseMessage> args) =>
                {
                    bool shouldHandle;
                    var exception = args.Outcome.Exception;
                    if (exception is TimeoutRejectedException ||
                       (exception is OperationCanceledException && exception.Source == "System.Private.CoreLib" && exception.InnerException is TimeoutException))
                    {
                        shouldHandle = true;
                    }
                    else if (args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        shouldHandle = false;
                    }
                    else
                    {
                        shouldHandle = HttpClientResiliencePredicates.IsTransient(args.Outcome);
                    }

                    return new ValueTask<bool>(shouldHandle);
                },
                Delay = options.RetryPolicyInitialWaitTime.ToTimespan(options.TimeUnits),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = options.RetryPolicyMaxRetries,
                UseJitter = true,
                OnRetry = args =>
                {
                    context.ServiceProvider.GetService<ILogger<T>>()?
                        .LogWarning(
                            "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. Status code {StatusCode}. {ExceptionMessage}",
                            typeof(T).Name,
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Result?.StatusCode,
                            args.Outcome.Exception.GetAllMessages());

                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.TooManyRequests),
                Delay = options.RetryPolicyTooManyAttemptsWaitTime.ToTimespan(options.TimeUnits),
                MaxRetryAttempts = options.RetryPolicyTooManyAttemptsMaxRetries,
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    context.ServiceProvider.GetService<ILogger<T>>()?
                        .LogWarning(
                            "{Type} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. Status code {StatusCode}. {ExceptionMessage}",
                            typeof(T).Name,
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Result?.StatusCode,
                            args.Outcome.Exception.GetAllMessages());

                    return default;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = options.Timeout.ToTimespan(options.TimeUnits)
            });
        };
    }
}
