using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace EPR.Subsidiary.Schedule.Function;

public class SubsidiaryBulkScheduler
{
    private readonly ILogger<SubsidiaryBulkScheduler> _logger;

    public SubsidiaryBulkScheduler(ILogger<SubsidiaryBulkScheduler> logger)
    {
        _logger = logger;
    }

    [FunctionName(nameof(SubsidiaryBulkScheduler))]
    public void Run([TimerTrigger("BulkInpectionSchedule")]TimerInfo myTimer, ILogger log)
    {
        _logger.LogInformation("C# timer trigger function ");
    }
}
