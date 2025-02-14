using EPR.SubsidiaryBulkUpload.Function.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddFeatureManagement();

        services.AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .ConfigureOptions(hostingContext.Configuration)
            .AddServices(hostingContext.Configuration)
            .AddAzureClients()
            .AddHttpClients(hostingContext.Configuration);
    })
    .Build();

await host.RunAsync();
