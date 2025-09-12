using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class SyncSubsidiariesFromRegistrationFileFunctionTests
{
    private const string LogPrefix = nameof(SyncSubsidiariesFromRegistrationFileFunction);
    private Mock<IFeatureManager> _featureManagerMock = null!;
    private Mock<ILogger<SyncSubsidiariesFromRegistrationFileFunction>> _loggerMock = null!;
    private Mock<IOrganisationService> _organisationServiceMock = null!;
    private SyncSubsidiariesFromRegistrationFileFunction _function = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _featureManagerMock = new Mock<IFeatureManager>();
        _loggerMock = new Mock<ILogger<SyncSubsidiariesFromRegistrationFileFunction>>();
        _organisationServiceMock = new Mock<IOrganisationService>();
        _function = new SyncSubsidiariesFromRegistrationFileFunction(_organisationServiceMock.Object, _featureManagerMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task Run_FeatureFlagDisabled_LogsAndExits()
    {
        // Arrange
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableSyncSubsidiariesFromRegistrationFile"))
            .ReturnsAsync(false);
        var nextScheduledTime = DateTime.UtcNow.AddHours(1);
        var timerInfo = new TimerInfo()
        {
            IsPastDue = false,
            ScheduleStatus = new ScheduleStatus
            {
                Next = nextScheduledTime
            }
        };

        // Act
        await _function.Run(timerInfo);

        // Assert
        _organisationServiceMock.Verify(service => service.SyncStagingToAccounts(), Times.Never);
        _featureManagerMock.Verify(f => f.IsEnabledAsync("EnableSyncSubsidiariesFromRegistrationFile"), Times.Once());
        _loggerMock.VerifyLog(x => x.LogInformation("{LogPrefix} Function is disabled by feature flag.", LogPrefix), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_FeatureFlagEnabled_WithScheduleStatus_LogsStartAndNextSchedule()
    {
        // Arrange
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableSyncSubsidiariesFromRegistrationFile"))
            .ReturnsAsync(true);

        var nextScheduledTime = DateTime.UtcNow.AddHours(1);
        var timerInfo = new TimerInfo
        {
            IsPastDue = false,
            ScheduleStatus = new ScheduleStatus
            {
                Next = nextScheduledTime
            }
        };

        // Act
        await _function.Run(timerInfo);

        // Assert
        _featureManagerMock.Verify(f => f.IsEnabledAsync("EnableSyncSubsidiariesFromRegistrationFile"), Times.Once());
        _loggerMock.VerifyLog(x => x.LogInformation(It.Is<string>(m => m.StartsWith($"{LogPrefix} Starting Sync Subsidiaries from Registration File to Accounts DB at")), Times.Once()));
        _loggerMock.VerifyLog(x => x.LogInformation("{LogPrefix} Next Sync Subsidiaries job scheduled at {NextTime}", LogPrefix, nextScheduledTime), Times.Once);
        _organisationServiceMock.Verify(service => service.SyncStagingToAccounts(), Times.Once);
    }
}