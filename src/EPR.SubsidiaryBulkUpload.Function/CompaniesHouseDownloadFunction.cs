using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseDownloadFunction
{
    private readonly ICompaniesHouseDownloadService companiesHouseDownloadService;
    private readonly ILogger<CompaniesHouseDownloadFunction> _logger;

    public CompaniesHouseDownloadFunction(ICompaniesHouseDownloadService companiesHouseDownloadService, ILogger<CompaniesHouseDownloadFunction> logger)
    {
        this.companiesHouseDownloadService = companiesHouseDownloadService;
        _logger = logger;
    }

    [Function("CompaniesHouseDownloadFunction")]
    public async Task Run([TimerTrigger("%CompaniesHouseDownload:Schedule%", RunOnStartup = true)] TimerInfo timerInfo)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {ExecutionTime}", DateTime.Now);

        await companiesHouseDownloadService.StartDownload();

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextTime}", timerInfo.ScheduleStatus.Next);
        }
    }
}
