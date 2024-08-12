using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class CompaniesHouseDownloadFunctionTests
{
    private Mock<ILogger<CompaniesHouseDownloadFunction>> _loggerMock;
    private CompaniesHouseDownloadFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<CompaniesHouseDownloadFunction>>();
        _systemUnderTest = new CompaniesHouseDownloadFunction(_loggerMock.Object);
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