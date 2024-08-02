using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class CompaniesHouseDownloadFunctionTests
{
    private Mock<ILogger<CompaniesHouseDownloadFunction>> _logger = null;
    private CompaniesHouseDownloadFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _logger = new Mock<ILogger<CompaniesHouseDownloadFunction>>();
        _systemUnderTest = new CompaniesHouseDownloadFunction(_logger.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseDownloadFunction_Accepts_Blob()
    {
        var content = "header1,header2\nval1,val2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var timerInfo = new TimerInfo()
        {
            IsPastDue = false,
            ScheduleStatus = new ScheduleStatus
            {
                Next = DateTime.UtcNow.AddHours(1)
            }
        };

        await _systemUnderTest.Run(timerInfo);
    }
}