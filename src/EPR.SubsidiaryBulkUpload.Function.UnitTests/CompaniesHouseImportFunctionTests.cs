using System.Text;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

public class CompaniesHouseImportFunctionTests
{
    private Mock<ILogger<CompaniesHouseImportFunction>> _logger = null;
    private CompaniesHouseImportFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _logger = new Mock<ILogger<CompaniesHouseImportFunction>>();
        _systemUnderTest = new CompaniesHouseImportFunction(_logger.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Accepts_Blob()
    {
        var content = "header1,header2\nval1,val2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        await _systemUnderTest.Run(stream, "test");
    }
}
