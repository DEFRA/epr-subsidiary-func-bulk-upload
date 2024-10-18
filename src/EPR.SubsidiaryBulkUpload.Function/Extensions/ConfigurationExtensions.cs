namespace EPR.SubsidiaryBulkUpload.Function.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Handlers;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Resilience;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
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
        services.Configure<AntivirusApiOptions>(configuration.GetSection(AntivirusApiOptions.SectionName));
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.Configure<CompaniesHouseDownloadOptions>(configuration.GetSection(CompaniesHouseDownloadOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<SubmissionApiOptions>(configuration.GetSection(SubmissionApiOptions.SectionName));
        services.Configure<TableStorageOptions>(configuration.GetSection(TableStorageOptions.SectionName));

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
        var sp = services.BuildServiceProvider();
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

        services.AddHttpClient<ISubmissionStatusClient, SubmissionStatusClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SubmissionApiOptions>>().Value;
            client.BaseAddress = new Uri($"{options.BaseUrl?.TrimEnd('/')}/v1/");
        })
            .AddPolicyHandler((services, _) => Policies.DefaultRetryPolicy<SubmissionStatusClient>(services));

        var antivirusOptions = services.BuildServiceProvider().GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

        services.AddHttpClient<IAntivirusClient, AntivirusClient>(client =>
        {
            client.BaseAddress = new Uri($"{antivirusOptions.BaseUrl}/v1/");
            client.BaseAddress = new Uri($"{antivirusOptions.BaseUrl?.TrimEnd('/')}/v1/");
            client.DefaultRequestHeaders.Add("OCP-APIM-Subscription-Key", antivirusOptions.SubscriptionKey);
        })
            .AddPolicyHandler((services, _) => Policies.AntivirusRetryPolicy<AntivirusClient>(services))
            .AddPolicyHandler((services, _) => Policies.AntivirusTimeoutPolicy(services))
            .AddHttpMessageHandler<AntivirusApiAuthorizationHandler>();

        services.AddHttpClient<IFileDownloadService, FileDownloadService>()
            .AddPolicyHandler((services, _) => Policies.CompaniesHouseDownloadRetryPolicy<FileDownloadService>(services));

        services.AddHttpClient<ISubsidiaryService, SubsidiaryService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
            c.BaseAddress = new Uri(config.SubsidiaryServiceBaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddHttpMessageHandler<AccountServiceAuthorisationHandler>();

        if (apiOptions.UseDirectCompaniesHouseLookup)
        {
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupDirectService>((sp, client) =>
            {
                var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

                client.BaseAddress = new Uri(apiOptions.CompaniesHouseDirectBaseUri);
                var apiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiOptions.CompaniesHouseDirectApiKey}:"));
                client.DefaultRequestHeaders.Add("Authorization", $"BASIC {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
                .AddCompaniesHouseResilienceHandler();
        }
        else
        {
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupService>((sp, client) =>
            {
                var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;

                client.BaseAddress = new Uri(apiOptions.CompaniesHouseLookupBaseUrl);
            })
                .ConfigurePrimaryHttpMessageHandler(GetClientCertificateHandler)
                .AddCompaniesHouseResilienceHandler();
        }

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
        var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
        services.AddSingleton<ITableStorageProcessor, TableStorageProcessor>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        services.AddScoped<AntivirusApiAuthorizationHandler>();
        services.AddScoped<ISystemDetailsProvider, SystemDetailsProvider>();

        services.AddTransient<AccountServiceAuthorisationHandler>();
        services.AddTransient<IAntivirusClient, AntivirusClient>();
        services.AddTransient<IBulkSubsidiaryProcessor, BulkSubsidiaryProcessor>();
        services.AddTransient<IBulkUploadOrchestration, BulkUploadOrchestration>();
        services.AddTransient<ICompaniesHouseDataProvider, CompaniesHouseDataProvider>();
        services.AddTransient<ICompaniesHouseDownloadService, CompaniesHouseDownloadService>();
        services.AddTransient<ICompaniesHouseFilePostService, CompaniesHouseFilePostService>();

        if (apiOptions.UseDirectCompaniesHouseLookup)
        {
            services.AddTransient<ICompaniesHouseLookupService, CompaniesHouseLookupDirectService>();
        }
        else
        {
            services.AddTransient<ICompaniesHouseLookupService, CompaniesHouseLookupService>();
        }

        services.AddTransient<ICsvProcessor, CsvProcessor>();
        services.AddTransient<IDownloadStatusStorage, DownloadStatusStorage>();
        services.AddTransient<IFileDownloadService, FileDownloadService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<IParserClass, ParserClass>();
        services.AddTransient<IRecordExtraction, RecordExtraction>();
        services.AddTransient<ISubsidiaryService, SubsidiaryService>();
        services.AddTransient<ISubmissionStatusClient, SubmissionStatusClient>();

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
