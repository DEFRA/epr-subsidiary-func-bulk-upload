using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Function.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class CompaniesHouseImportFunctionTests
{
    private const string BlobContainerName = "test_container";
    private const string CsvBlobName = "test-2024-07-01.csv";
    private const int CsvRowCount = 2;
    private const string CsvContent =
        """
        CompanyName,CompanyNumber\n
        SON LTD,19910001\n
        DAUGHTER LTD,19910001\n
        """;

    private Mock<BlobClient> _blobClientMock;
    private Mock<ICsvProcessor> _csvProcessorMock;
    private Mock<ITableStorageProcessor> _tableStorageProcessor;
    private Mock<ILogger<CompaniesHouseImportFunction>> _loggerMock;
    private Mock<IOptions<TableStorageOptions>> _configOptionsMock;
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

        _tableStorageProcessor = new Mock<ITableStorageProcessor>();

        _loggerMock = new Mock<ILogger<CompaniesHouseImportFunction>>();
        _configOptionsMock = new Mock<IOptions<TableStorageOptions>>();
        var options = new TableStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            CompaniesHouseOfflineDataTableName = "CompaniesHouseData"
        };
        _configOptionsMock.Setup(x => x.Value).Returns(options);
        _systemUnderTest = new CompaniesHouseImportFunction(_loggerMock.Object, _csvProcessorMock.Object, _tableStorageProcessor.Object, _configOptionsMock.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Calls_CsvService()
    {
        // Arrange
        _csvProcessorMock.Setup(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()))
            .ReturnsAsync(new List<CompanyHouseTableEntity> { new() { CompanyName = "test" }, new() { CompanyName = "test2" } });

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()), Times.Once);
        _tableStorageProcessor.Verify(x => x.WriteToAzureTableStorage(It.IsAny<List<CompanyHouseTableEntity>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Logs_Result()
    {
        // Arrange
        _csvProcessorMock.Setup(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()))
            .ReturnsAsync(new List<CompanyHouseTableEntity> { new() { CompanyName = "test" }, new() { CompanyName = "test2" } });

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("Blob {Name} has metadata {Key} {Value}", CsvBlobName, "test", "test"), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("CompaniesHouseImport blob trigger processed {Count} records from csv blob {Name}", CsvRowCount, CsvBlobName), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Logs_Result_When_NoPartitionKeyInFileName()
    {
        // Arrange
        _blobClientMock
            .SetupGet(m => m.Name)
            .Returns("test.csv");

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("CompaniesHouseImport blob trigger function did not process file because name '{Name}' doesn't contain partition key", "test.csv"), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Deletes_Blob_And_Logs_Result()
    {
        // Arrange
        var mockResponse = new Mock<Response<bool>>();
        mockResponse.SetupGet(x => x.Value).Returns(true);

        var response = Response.FromValue(true, new Mock<Response>().Object);

        _blobClientMock
            .Setup(client => client.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _blobClientMock.Verify(client => client.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once());

        _loggerMock.VerifyLog(x => x.LogInformation("Blob {Name} was deleted.", CsvBlobName), Times.Once);
    }
}
