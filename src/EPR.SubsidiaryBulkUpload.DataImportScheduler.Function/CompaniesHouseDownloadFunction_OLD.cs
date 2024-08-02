using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.DataImportScheduler.Function;

public class CompaniesHouseDownloadFunction_OLD
{
    private readonly ILogger _logger;

    public CompaniesHouseDownloadFunction_OLD(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CompaniesHouseDownloadFunction>();
    }

    [Function("CompaniesHouseDownloadFunction")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {ExecutionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextTime}", myTimer.ScheduleStatus.Next);
        }
    }
}
