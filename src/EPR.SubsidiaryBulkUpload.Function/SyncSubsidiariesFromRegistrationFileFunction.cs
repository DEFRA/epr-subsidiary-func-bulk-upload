using EPR.SubsidiaryBulkUpload.Application.Constants;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace EPR.SubsidiaryBulkUpload.Function;

public class SyncSubsidiariesFromRegistrationFileFunction(IOrganisationService organisationService, IFeatureManager featureManager, ILogger<SyncSubsidiariesFromRegistrationFileFunction> logger)
{
    private const string LogPrefix = nameof(SyncSubsidiariesFromRegistrationFileFunction);

    [Function("SyncSubsidiariesFromRegistrationFileFunction")]
    public async Task Run([TimerTrigger("%SyncSubsidiariesFromRegistrationFile:Schedule%", RunOnStartup = true)] TimerInfo timerInfo)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableSyncSubsidiariesFromRegistrationFile))
        {
            logger.LogInformation("{LogPrefix} Function is disabled by feature flag.", LogPrefix);
            return;
        }

        logger.LogInformation("{LogPrefix} Starting Sync Subsidiaries from Registration File to Accounts DB at {ExecutionTime}", LogPrefix, DateTime.Now);

        var result = await organisationService.SyncStagingToAccounts();
        logger.LogInformation("{LogPrefix} Synced Subsidiaries from Registration File to Accounts DB. Successfully Processed {SuccessCount} records. Failed to process {FailureCount} records", LogPrefix, result?.NumProcessedRecords, result?.NumErroredRecords);

        if (timerInfo.ScheduleStatus?.Next is not null)
        {
            logger.LogInformation("{LogPrefix} Next Sync Subsidiaries job scheduled at {NextTime}", LogPrefix, timerInfo.ScheduleStatus.Next);
        }
    }
}