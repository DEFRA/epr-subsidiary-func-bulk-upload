using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Function;

public class CompaniesHouseDownloadFunction
{
    private readonly ILogger<CompaniesHouseDownloadFunction> _logger;

    public CompaniesHouseDownloadFunction(ILogger<CompaniesHouseDownloadFunction> logger)
    {
        _logger = logger;
    }

    [Function("CompaniesHouseDownloadFunction")]
    public async Task Run([TimerTrigger("%CompaniesHouseDownload:Schedule%", RunOnStartup = true)] TimerInfo timerInfo)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {ExecutionTime}", DateTime.Now);

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextTime}", timerInfo.ScheduleStatus.Next);
        }
    }
}
