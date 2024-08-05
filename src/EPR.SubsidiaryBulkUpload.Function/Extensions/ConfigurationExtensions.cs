namespace EPR.SubsidiaryBulkUpload.Function.Extensions;

using System.Diagnostics.CodeAnalysis;
using Application.Clients;
using Application.Clients.Interfaces;
using Application.Handlers;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // services.ConfigureSection<SubmissionStatusApiOptions>(SubmissionStatusApiOptions.Section);
        // services.ConfigureSection<AntivirusApiOptions>(AntivirusApiOptions.Section);
        // services.ConfigureSection<AntivirusApiOptions>(AntivirusApiOptions.Section);
        services.Configure<AntivirusApiOptions>(configuration.GetSection(AntivirusApiOptions.Section));

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // var sp = services.BuildServiceProvider();

        /*
        services.AddHttpClient<ISubmissionStatusApiClient, SubmissionStatusApiClient>((sp, c) =>
        {
            var submissionStatusApiOptions = sp.GetRequiredService<IOptions<SubmissionStatusApiOptions>>().Value;
            c.BaseAddress = new Uri($"{submissionStatusApiOptions.BaseUrl}/v1/");
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        */

        services.AddHttpClient<IAntivirusApiClient, AntivirusApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

            client.BaseAddress = new Uri($"{options.BaseUrl}/v1/");
            client.Timeout = TimeSpan.FromSeconds(options.Timeout);
            client.DefaultRequestHeaders.Add("OCP-APIM-Subscription-Key", options.SubscriptionKey);
        }).AddHttpMessageHandler<TradeAntivirusApiAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAntivirusApiClient, AntivirusApiClient>();
        services.AddTransient<ICsvProcessor, CsvProcessor>();
        services.AddTransient<TradeAntivirusApiAuthorizationHandler>();

        return services;
    }
}