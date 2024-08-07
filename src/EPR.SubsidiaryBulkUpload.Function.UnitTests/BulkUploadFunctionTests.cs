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
public class BulkUploadFunctionTests
{
    private const string BlobContainerName = "test_container";
    private const string CsvBlobName = "test.csv";
    private const int CsvRowCount = 2;
    private const string CsvContent =
        """
        organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n
        100000,,PARENT LIMITED,09910000,Parent,\n
        100001,SON LTD,19910001,Child,\n
        100001,12345,DAUGHTER LTD,19910001,Child,\n
        """;

    private Mock<BlobClient> _blobClientMock;
    private Mock<ICsvProcessor> _csvProcessorMock;
    private Mock<ILogger<BulkUploadFunction>> _loggerMock;

    private BulkUploadFunction _systemUnderTest;

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

        _loggerMock = new Mock<ILogger<BulkUploadFunction>>();

        _systemUnderTest = new BulkUploadFunction(null, null, null, null);
    }

    [TestMethod]
    public async Task BulkUploadFunction_Calls_CsvService()
    {
        // Act
        await _systemUnderTest.Run(null, string.Empty);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream(It.IsAny<Stream>()), Times.Once);
    }

    [TestMethod]
    public async Task BulkUploadFunction_Logs_Result()
    {
        // Act
        await _systemUnderTest.Run(null, string.Empty);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("C# Blob trigger processed {Count} records from csv blob {Name}", CsvRowCount, CsvBlobName), Times.Once);
    }
}