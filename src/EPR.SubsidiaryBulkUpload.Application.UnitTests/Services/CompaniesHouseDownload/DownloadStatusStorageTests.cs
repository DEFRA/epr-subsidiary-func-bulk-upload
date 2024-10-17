using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class DownloadStatusStorageTests
{
    private Fixture fixture;
    private Mock<TableServiceClient> tableServiceClient;
    private Mock<TableClient> tableClient;
    private FakeTimeProvider timeProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
        tableServiceClient = new Mock<TableServiceClient>();
        tableClient = new Mock<TableClient>();
        tableServiceClient
            .Setup(tsc => tsc.GetTableClient(DownloadStatusStorage.CompaniesHouseDownloadTableName))
            .Returns(tableClient.Object);
        timeProvider = new();
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadStatusAsync_ShouldReturnTrue()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = "test_file_1.zip" },
            new() { DownloadFileName = "test_file_2.zip" },
            new() { DownloadFileName = "test_file_3.zip" }
        };

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        actual.Should().BeTrue();
        tableClient.Verify(tc => tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
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
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadStatusAsync_ShouldReplyFalseGetCompaniesStatusCreateTableFails()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        tableClient.Setup(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync(partitionKey);

        // Assert
        actual.Should().BeFalse();
    }

    [TestMethod]
    public async Task SetCompaniesHouseFileDownloadStatusAsync_ShouldUploadStatusAndReplySuccess()
    {
        // Arrange
        var status = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var response = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(status);

        // Assert
        response.Should().Be(true);
        tableClient.Verify(tc => tc.UpsertEntityAsync<CompaniesHouseFileSetDownloadStatus>(status, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ShouldCreateLogIfEmpty()
    {
        // Arrange
        var partitionKey = "202403";

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey);

        // Assert
        tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        tableClient.Verify(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task CreateCompaniesHouseFileDownloadLogAsync_ThrowsException()
    {
        // Arrange
        var partitionKey = "202403";

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        tableClient.Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<RequestFailedException>(async () =>
            await downloadStatusStorage.CreateCompaniesHouseFileDownloadLogAsync(partitionKey));

        tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        tableClient.Verify(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), It.IsAny<CancellationToken>()));
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

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(downloadLog));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

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

        tableClient.SetupSequence(tc =>
            tc.QueryAsync<CompaniesHouseFileSetDownloadStatus>(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompaniesHouseFileSetDownloadStatus>(new List<CompaniesHouseFileSetDownloadStatus>()));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

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

        tableClient.Setup(tc => tc.QueryAsync(
                It.IsAny<Expression<Func<CompaniesHouseFileSetDownloadStatus, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

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
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        var companiesHouseFileSetDownloadStatus = new CompaniesHouseFileSetDownloadStatus
        {
            RowKey = "Part-1-03-2023",
            PartitionKey = partitionKey,
            Timestamp = timeProvider.GetUtcNow(),
            DownloadStatus = FileDownloadResponseCode.Succeeded,
            DownloadFileName = "test_file_1.zip"
        };

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var result = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(companiesHouseFileSetDownloadStatus);

        // Assert
        result.Should().BeTrue();
        tableClient.Verify(tc => tc.UpsertEntityAsync(companiesHouseFileSetDownloadStatus, TableUpdateMode.Merge, It.IsAny<CancellationToken>()), Times.Once);
    }
}
