using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class CompaniesHouseDownloadFunctionTests
{
    private Mock<ILogger<CompaniesHouseDownloadFunction>> _loggerMock;
    private CompaniesHouseDownloadFunction _systemUnderTest;
    private Mock<ICompaniesHouseDownloadService> _downloadService;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<CompaniesHouseDownloadFunction>>();
        _downloadService = new Mock<ICompaniesHouseDownloadService>();
        _systemUnderTest = new CompaniesHouseDownloadFunction(_downloadService.Object, _loggerMock.Object);
    }

    [TestMethod]
    public void ShouldStartTheDownload()
    {
        // Arrange
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
        _systemUnderTest.Run(timerInfo);

        // Assert
        _downloadService.Verify(ds => ds.StartDownload());
    }

    [TestMethod]
    public async Task CompaniesHouseDownloadFunction_Logs_Result()
    {
        // Arrange
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
        await _systemUnderTest.Run(timerInfo);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("Next timer schedule at: {NextTime}", nextScheduledTime), Times.Once);
    }
}