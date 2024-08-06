using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Function.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class CompaniesHouseImportFunctionTests
{
    private const string BlobContainerName = "test_container";
    private const string CsvBlobName = "test.csv";
    private const int CsvRowCount = 2;
    private const string CsvContent =
        """
        CompanyName,CompanyNumber\n
        SON LTD,19910001\n
        DAUGHTER LTD,19910001\n
        """;

    private Mock<BlobClient> _blobClientMock;
    private Mock<ICsvProcessor> _csvProcessorMock;
    private Mock<ILogger<CompaniesHouseImportFunction>> _loggerMock;
    private CompaniesHouseImportFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _blobClientMock = new Mock<BlobClient>();
        _blobClientMock
            .SetupGet(m => m.BlobContainerName)
            .Returns(BlobContainerName);
        _blobClientMock
            .SetupGet(m => m.Name)
            .Returns(CsvBlobName);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var responseMock = new Mock<Response>();

        var downloadStreamingDetails = BlobsModelBuilder.CreateBlobDownloadDetails(
            CsvContent.Length,
            new Dictionary<string, string> { { "test", "test" } });

        var downloadStreamingResult = BlobsModelFactory.BlobDownloadStreamingResult(stream, downloadStreamingDetails);

        var response = Response.FromValue(downloadStreamingResult, new Mock<Response>().Object);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _csvProcessorMock = new Mock<ICsvProcessor>();
        _csvProcessorMock.Setup(x => x.ProcessStream(It.IsAny<Stream>()))
            .ReturnsAsync(CsvRowCount);

        _loggerMock = new Mock<ILogger<CompaniesHouseImportFunction>>();
        _systemUnderTest = new CompaniesHouseImportFunction(_loggerMock.Object, _csvProcessorMock.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Calls_CsvService()
    {
        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream(It.IsAny<Stream>()), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunctionn_Logs_Result()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", CsvRowCount, CsvBlobName), Times.Once);
    }
}
