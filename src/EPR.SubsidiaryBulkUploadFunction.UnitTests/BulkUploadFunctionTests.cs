using System.Text;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUploadFunction.UnitTests;

[TestClass]
public class BulkUploadFunctionTests
{
    private Mock<ILogger<BulkUploadFunction>> _logger = null;
    private BulkUploadFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _logger = new Mock<ILogger<BulkUploadFunction>>();
        _systemUnderTest = new BulkUploadFunction(_logger.Object);
    }

    [TestMethod]
    public async Task BulkUploadFunction_Accepts_Blob()
    {
        var content = "header1,header2\nval1,val2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        await _systemUnderTest.Run(stream, "test");
    }
}