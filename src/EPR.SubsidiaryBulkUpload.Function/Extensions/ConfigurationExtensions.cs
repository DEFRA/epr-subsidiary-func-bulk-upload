namespace EPR.SubsidiaryBulkUpload.Function.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using EPR.SubsidiaryBulkUpload.Application.Configs;
using EPR.SubsidiaryBulkUpload.Application.Handlers;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.Configure<TableStorageOptions>(configuration.GetSection(TableStorageOptions.SectionName));
        services.Configure<RedisConfig>(configuration.GetSection(RedisConfig.SectionName));
        return services;
    }

    public static IServiceCollection AddAzureClients(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();

        var tableStorageOptions = sp.GetRequiredService<IOptions<TableStorageOptions>>();

        services.AddAzureClients(cb =>
        {
            cb.AddTableServiceClient(tableStorageOptions.Value.ConnectionString);
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ISubsidiaryService, SubsidiaryService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
            c.BaseAddress = new Uri(config.SubsidiaryServiceBaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<AccountServiceAuthorisationHandler>();

        var isDevMode = configuration.GetValue<bool?>("ApiConfig:DeveloperMode");
        if (isDevMode is true)
        {
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupDirectService>((sp, client) =>
            {
                var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

                client.BaseAddress = new Uri(apiOptions.CompaniesHouseDirectBaseUri);
                var apiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiOptions.CompaniesHouseDirectApiKey}:"));
                client.DefaultRequestHeaders.Add("Authorization", $"BASIC {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }
        else
        {
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupService>((sp, client) =>
            {
                var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

                client.BaseAddress = new Uri(apiOptions.CompaniesHouseLookupBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(apiOptions.Timeout);
            })
                .ConfigurePrimaryHttpMessageHandler(GetClientCertificateHandler);
        }

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var redisConfig = sp.GetRequiredService<IOptions<RedisConfig>>().Value;

        services.AddTransient<AccountServiceAuthorisationHandler>();
        services.AddTransient<IBulkUploadOrchestration, BulkUploadOrchestration>();
        services.AddTransient<IBulkSubsidiaryProcessor, BulkSubsidiaryProcessor>();
        services.AddTransient<ICompaniesHouseDataProvider, CompaniesHouseDataProvider>();
        services.AddTransient<IRecordExtraction, RecordExtraction>();
        services.AddTransient<ICsvProcessor, CsvProcessor>();
        services.AddTransient<IParserClass, ParserClass>();
        services.AddTransient<ITableStorageProcessor, TableStorageProcessor>();
        services.AddTransient<ISubsidiaryService, SubsidiaryService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig.ConnectionString));

        var isDevMode = configuration.GetValue<bool?>("ApiConfig:DeveloperMode");
        if (isDevMode is true)
        {
            services.AddTransient<ICompaniesHouseLookupService, CompaniesHouseLookupDirectService>();
        }
        else
        {
            services.AddTransient<ICompaniesHouseLookupService, CompaniesHouseLookupService>();
        }

        return services;
    }

    private static HttpMessageHandler GetClientCertificateHandler(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentException("ServiceProvider must not be null");
        }

        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.SslProtocols = SslProtocols.Tls12;
        handler.ClientCertificates
            .Add(new X509Certificate2(
                Convert.FromBase64String(sp.GetRequiredService<IOptions<ApiOptions>>().Value.Certificate)));

        return handler;
    }
}