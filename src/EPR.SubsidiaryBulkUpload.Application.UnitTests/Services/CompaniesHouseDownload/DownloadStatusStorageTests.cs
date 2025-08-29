using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class DownloadStatusStorageTests
{
    private Fixture _fixture;
    private Mock<ILogger<DownloadStatusStorage>> _loggerMock;
    private Mock<TableServiceClient> _tableServiceClient;
    private Mock<TableClient> _tableClient;
    private FakeTimeProvider _timeProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _loggerMock = new Mock<ILogger<DownloadStatusStorage>>();
        _tableServiceClient = new Mock<TableServiceClient>();
        _tableClient = new Mock<TableClient>();
        _tableServiceClient
            .Setup(tsc => tsc.GetTableClient(DownloadStatusStorage.CompaniesHouseDownloadTableName))
            .Returns(_tableClient.Object);
        _timeProvider = new();
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadStatusAsync_ShouldReturnTrue()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = "test_file_1.zip" },
            new() { DownloadFileName = "test_file_2.zip" },
            new() { DownloadFileName = "test_file_3.zip" }
        };

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        actual.Should().BeTrue();
        _tableClient.Verify(tc => tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadStatusAsync_ShouldTryCreateTable()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        _tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadStatusAsync_ShouldReplyFalseGetCompaniesStatusCreateTableFails()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        _tableClient.Setup(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        actual.Should().BeFalse();
    }

    [TestMethod]
    public async Task SetCompaniesHouseFileDownloadStatusAsync_ShouldUploadStatusAndReplySuccess()
    {
        // Arrange
        var status = _fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var response = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(status);

        // Assert
        response.Should().Be(true);
        _tableClient.Verify(tc => tc.UpsertEntityAsync<CompaniesHouseFileSetDownloadStatus>(status, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task SetCompaniesHouseFileDownloadStatusAsync_LogsError_When_RequestFailedException()
    {
        // Arrange
        var status = _fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        _tableClient.Setup(tc => tc.UpsertEntityAsync(It.IsAny<CompaniesHouseFileSetDownloadStatus>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        // Act
        var response = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(status);

        // Assert
        response.Should().Be(false);
        _tableClient.Verify(tc => tc.UpsertEntityAsync<CompaniesHouseFileSetDownloadStatus>(status, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        _loggerMock.VerifyLog(x => x.LogError(It.IsAny<Exception>(), "Cannot get or create table {TableName}", DownloadStatusStorage.CompaniesHouseDownloadTableName), Times.Once);
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ShouldCreateLogIfEmpty()
    {
        // Arrange
        var partitionKey = "202403";

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey, 7);

        // Assert
        _tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        _tableClient.Verify(tc => tc.SubmitTransactionAsync(It.Is<IEnumerable<TableTransactionAction>>(x => x.Count() == 7), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ShouldRecreateMissingLogEntries()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = "202403";

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { RowKey = "Part-1-03-2024", DownloadFileName = "test_file_1.zip" },
            new() { RowKey = "Part-3-03-2024", DownloadFileName = "test_file_3.zip" },
            new() { RowKey = "Part-5-03-2024", DownloadFileName = "test_file_5.zip" }
        };

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey, 5);

        // Assert
        _tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        _tableClient.Verify(tc => tc.SubmitTransactionAsync(It.Is<IEnumerable<TableTransactionAction>>(x => x.Count() == 2), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ShouldNotSubmitTransactionsIfNothingToUpdate()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = "202403";

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { RowKey = "Part-1-03-2024", DownloadFileName = "test_file_1.zip" },
            new() { RowKey = "Part-3-03-2024", DownloadFileName = "test_file_3.zip" },
            new() { RowKey = "Part-5-03-2024", DownloadFileName = "test_file_5.zip" }
        };

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey, 3);

        // Assert
        _tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        _tableClient.Verify(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ThrowsException()
    {
        // Arrange
        var partitionKey = "202403";

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        _tableClient.Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<FileDownloadException>(async () =>
            await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey, 7));

        _tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        _tableClient.Verify(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadListAsync_ShouldReturnDownloadLog()
    {
        // Arrange
        var partitionKey = "202403";

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = "test_file_1.zip" },
            new() { DownloadFileName = "test_file_2.zip" },
            new() { DownloadFileName = "test_file_3.zip" }
        };

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var result = await downloadStatusStorage.GetCompaniesHouseFileDownloadListAsync(partitionKey);

        // Assert
        result.Count.Should().Be(3);
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadListAsync_ContainNoData()
    {
        // Arrange
        var partitionKey = "199912";

        _tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var result = await downloadStatusStorage.GetCompaniesHouseFileDownloadListAsync(partitionKey);

        // Assert
        result.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadListAsync_ReturnsNoDataWhenThrowsException()
    {
        // Arrange
        var partitionKey = "199912";

        _tableClient.Setup(tc => tc.QueryAsync(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var result = await downloadStatusStorage.GetCompaniesHouseFileDownloadListAsync(partitionKey);

        // Assert
        result.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task SetCompaniesHouseFileDownloadStatusAsync_ShouldUpdateLog()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        var companiesHouseFileSetDownloadStatus = new CompaniesHouseFileSetDownloadStatus
        {
            RowKey = "Part-1-03-2023",
            PartitionKey = partitionKey,
            Timestamp = _timeProvider.GetUtcNow(),
            DownloadStatus = FileDownloadResponseCode.Succeeded,
            DownloadFileName = "test_file_1.zip"
        };

        var downloadStatusStorage = new DownloadStatusStorage(_tableServiceClient.Object, _timeProvider, _loggerMock.Object);

        // Act
        var result = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(companiesHouseFileSetDownloadStatus);

        // Assert
        result.Should().BeTrue();
        _tableClient.Verify(tc => tc.UpsertEntityAsync(companiesHouseFileSetDownloadStatus, TableUpdateMode.Merge, It.IsAny<CancellationToken>()), Times.Once);
    }
}
