﻿using EPR.Common.Logging.Extensions;
using EPR.SubsidiaryBulkUpload.Function.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .ConfigureOptions(hostingContext.Configuration)
            .AddServices()
            .AddHttpClients()
            .ConfigureLogging();
    })
    .Build();

await host.RunAsync();