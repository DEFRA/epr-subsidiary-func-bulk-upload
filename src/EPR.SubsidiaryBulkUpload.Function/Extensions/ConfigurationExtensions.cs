namespace EPR.SubsidiaryBulkUpload.Function.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using EPR.SubsidiaryBulkUpload.Application.Configs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        /*
        services.Configure<AntivirusApiOptions>(configuration.GetSection(AntivirusApiOptions.Section));
        */
        services.Configure<ConfigOptions>(options =>
        {
            options.TableStorageConnectionString = configuration["TableStorage:ConnectionString"];
            options.CompaniesHouseOfflineDataTableName = configuration["CompaniesHouseOfflineData:TableName"];
        });

        services.Configure<ApiConfig>(configuration.GetSection(ApiConfig.SectionName));
        services.Configure<HttpClientOptions>(configuration.GetSection(HttpClientOptions.ConfigSection));
        return services;
    }

    public static IServiceCollection AddAzureClients(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();

        /*
        var blobStorageOptions = sp.GetRequiredService<IOptions<BlobStorageOptions>>();

        services.AddAzureClients(cb =>
        {
            cb.AddBlobServiceClient(blobStorageOptions.Value.ConnectionString);
        });
        */

        var configOptions = sp.GetRequiredService<IOptions<ConfigOptions>>();

        services.AddAzureClients(cb =>
        {
            cb.AddTableServiceClient(configOptions.Value.TableStorageConnectionString);
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
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

        /*
        services.AddHttpClient<IAntivirusApiClient, AntivirusApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AntivirusApiOptions>>().Value;

            client.BaseAddress = new Uri($"{options.BaseUrl}/v1/");
            client.Timeout = TimeSpan.FromSeconds(options.Timeout);
            client.DefaultRequestHeaders.Add("OCP-APIM-Subscription-Key", options.SubscriptionKey);
        }).AddHttpMessageHandler<TradeAntivirusApiAuthorizationHandler>();
        */

        /*
        services.AddTransient<AccountServiceAuthorisationHandler>();

        services.AddHttpClient<ISubsidiaryService, SubsidiaryService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<ApiConfig>>().Value;

            client.BaseAddress = new Uri(config.SubsidiaryServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.Timeout);
        })
        .AddHttpMessageHandler(AccountServiceAuthorisationHandler);
        */

        services.AddHttpClient<ISubsidiaryService, SubsidiaryService>((sp, c) =>
        {
            var config = sp.GetRequiredService<IOptions<ApiConfig>>().Value;
            c.BaseAddress = new Uri(config.SubsidiaryServiceBaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var isDevMode = configuration["ApiConfig:DeveloperMode"]; // configuration.GetValue<bool>("DeveloperMode");
        if (isDevMode == "true")
        {
            const string CompaniesHouseClient = "CompaniesHouse";
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupDirectService>(CompaniesHouseClient, client =>
            {
                client.BaseAddress = new Uri(configuration["CompaniesHouseApi:BaseUri"]);
                var apiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["CompaniesHouseApi:ApiKey"]}:"));
                client.DefaultRequestHeaders.Add("Authorization", $"BASIC {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }
        else
        {
            services.AddHttpClient<ICompaniesHouseLookupService, CompaniesHouseLookupService>((sp, client) =>
            {
            var apiOptions = sp.GetRequiredService<IOptions<ApiConfig>>().Value;
            var httpClientOptions = sp.GetRequiredService<IOptions<HttpClientOptions>>().Value;

            client.BaseAddress = new Uri(apiOptions.CompaniesHouseLookupBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.Timeout);
            })
                .ConfigurePrimaryHttpMessageHandler(GetClientCertificateHandler);
        }

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IBulkUploadOrchestration, BulkUploadOrchestration>();
        services.AddTransient<IBulkSubsidiaryProcessor, BulkSubsidiaryProcessor>();
        services.AddTransient<ICompaniesHouseDataProvider, CompaniesHouseDataProvider>();
        services.AddTransient<IRecordExtraction, RecordExtraction>();
        services.AddTransient<ICsvProcessor, CsvProcessor>();
        services.AddTransient<ICompaniesHouseCsvProcessor, CompaniesHouseCsvProcessor>();
        services.AddTransient<ITableStorageProcessor, TableStorageProcessor>();
        services.AddTransient<IAzureStorageTableService, AzureStorageTableService>();
        services.AddTransient<ISubsidiaryService, SubsidiaryService>();
        services.AddTransient<IRedisNotificationService, RedisNotificationService>();

        var isDevMode = configuration["ApiConfig:DeveloperMode"]; // configuration.GetValue<bool>("DeveloperMode");
        if (isDevMode == "true")
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
                Convert.FromBase64String(sp.GetRequiredService<IOptions<ApiConfig>>().Value.Certificate)));

        return handler;
    }
}