using System.IO.Compression;
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
    private const string CsvBlobName = "Test-2024-10-01-part1_7.csv";
    private const string ZipBlobName = "Test-2024-10-01-part1_7.zip";
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

        _loggerMock = new Mock<ILogger<CompaniesHouseImportFunction>>();
        _configOptionsMock = new Mock<IOptions<TableStorageOptions>>();
        var options = new TableStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            CompaniesHouseOfflineDataTableName = "CompaniesHouseData"
        };
        _configOptionsMock.Setup(x => x.Value).Returns(options);

        _csvProcessorMock = new Mock<ICsvProcessor>();
        _tableStorageProcessor = new Mock<ITableStorageProcessor>();

        _csvProcessorMock.Setup(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()))
            .ReturnsAsync(new List<CompanyHouseTableEntity> { new() { CompanyName = "test" }, new() { CompanyName = "test2" } });

        _systemUnderTest = new CompaniesHouseImportFunction(_loggerMock.Object, _csvProcessorMock.Object, _tableStorageProcessor.Object, _configOptionsMock.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Calls_CsvService_ForCsvFile()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var response = CreateDownloadStreamingResponse(stream, CsvBlobName);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()), Times.Once);
        _tableStorageProcessor.Verify(x => x.WriteToAzureTableStorage(It.IsAny<List<CompanyHouseTableEntity>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Calls_CsvService_ForZipFile()
    {
        // Arrange
        var archiveStream = new MemoryStream();
        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
        {
            var entry = zipArchive.CreateEntry(ZipBlobName, CompressionLevel.Fastest);
            using (var entryStream = entry.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(CsvContent);
                await entryStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        archiveStream.Seek(0, SeekOrigin.Begin);
        var response = CreateDownloadStreamingResponse(archiveStream, ZipBlobName);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()), Times.Once);
        _tableStorageProcessor.Verify(x => x.WriteToAzureTableStorage(It.IsAny<List<CompanyHouseTableEntity>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_DoesNotCallTableStorageProcessor_When_CsvService_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var response = CreateDownloadStreamingResponse(stream, CsvBlobName);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _csvProcessorMock.Setup(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()))
            .ReturnsAsync((List<CompanyHouseTableEntity>)null);

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _csvProcessorMock.Verify(x => x.ProcessStream<CompanyHouseTableEntity>(It.IsAny<Stream>(), It.IsAny<CsvConfiguration>()), Times.Once);
        _tableStorageProcessor.Verify(x => x.WriteToAzureTableStorage(It.IsAny<List<CompanyHouseTableEntity>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Logs_Result()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var response = CreateDownloadStreamingResponse(stream, CsvBlobName);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _systemUnderTest.Run(_blobClientMock.Object);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("CompaniesHouseImport blob trigger processed {Count} records from csv blob {Name}", CsvRowCount, CsvBlobName), Times.Once);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Logs_Result_When_NoPartitionKeyInFileName()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var response = CreateDownloadStreamingResponse(stream, null);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
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
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvContent));
        var response = CreateDownloadStreamingResponse(stream, CsvBlobName);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

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

    private Response<BlobDownloadStreamingResult> CreateDownloadStreamingResponse(Stream stream, string fileName = null)
    */
        var metadata = new Dictionary<string, string>();
        if (fileName is not null)
        {
            metadata.Add("fileName", fileName);
        }

        var downloadStreamingDetails = BlobsModelBuilder.CreateBlobDownloadDetails(
            (int)stream.Length, /* CsvContent.Length, */
            new Dictionary<string, string> { { "fileName", fileName } });

        var downloadStreamingResult = BlobsModelFactory.BlobDownloadStreamingResult(stream, downloadStreamingDetails);

        var response = Response.FromValue(downloadStreamingResult, new Mock<Response>().Object);
        _blobClientMock.Setup(client => client.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return response;
    }
}
