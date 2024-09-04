using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
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
    public async Task ShouldGetTheLateststatus()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var rowKey = $"{DownloadStatusStorage.MonthPartialRowKey}-{month}-{year}";

        var downloadStatus = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var tableResponse = new Mock<Response<CompaniesHouseFileSetDownloadStatus>>();
        tableResponse.Setup(r => r.Value).Returns(downloadStatus);

        tableClient.Setup(tc => tc.GetEntityAsync<CompaniesHouseFileSetDownloadStatus>(
                DownloadStatusStorage.CompaniesHouseDownloadPartitionKey, rowKey, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tableResponse.Object);

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        // Assert
        actual.Should().BeEquivalentTo(downloadStatus);
    }

    [TestMethod]
    [DataRow(2024, 3, 2024, 2)]
    [DataRow(2024, 1, 2023, 12)]
    public async Task ShouldGetPreviousMonthsCountWhenNothingSetForThisMonth(int thisYear, int thisMonth, int perviousYear, int previousMonth)
    {
        // Arrange
        var now = new DateTimeOffset(thisYear, thisMonth, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var previousMonthExpectedFileCount = 33;

        var currentMonthRowKey = $"{DownloadStatusStorage.MonthPartialRowKey}-{thisMonth}-{thisYear}";
        var previousMonthRowKey = $"{DownloadStatusStorage.MonthPartialRowKey}-{previousMonth}-{perviousYear}";

        fixture.Customize<CompaniesHouseFileSetDownloadStatus>(ctx => ctx.With(s => s.CurrentRunExpectedFileCount, previousMonthExpectedFileCount));
        var previousDownloadStatus = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var previousMonthTableResponse = new Mock<Response<CompaniesHouseFileSetDownloadStatus>>();
        previousMonthTableResponse.Setup(r => r.Value).Returns(previousDownloadStatus);

        var currentMonthResponse = new Mock<Response<CompaniesHouseFileSetDownloadStatus>>();
        currentMonthResponse.Setup(r => r.Value).Returns<CompaniesHouseFileSetDownloadStatus>(null);

        tableClient.Setup(tc => tc.GetEntityAsync<CompaniesHouseFileSetDownloadStatus>(
                DownloadStatusStorage.CompaniesHouseDownloadPartitionKey, currentMonthRowKey, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentMonthResponse.Object);

        tableClient.Setup(tc => tc.GetEntityAsync<CompaniesHouseFileSetDownloadStatus>(
                DownloadStatusStorage.CompaniesHouseDownloadPartitionKey, previousMonthRowKey, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousMonthTableResponse.Object);

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        // Assert
        actual.Should().NotBeNull();
        actual.CurrentRunExpectedFileCount.Should().Be(previousMonthExpectedFileCount);
    }

    [TestMethod]
    public async Task ShouldGetSeedValueWhenFailsToGetEntitiesDueToExceptions()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var response = new Mock<Response<CompaniesHouseFileSetDownloadStatus>>();
        response.Setup(r => r.Value).Returns<CompaniesHouseFileSetDownloadStatus>(null);

        tableClient.Setup(tc => tc.GetEntityAsync<CompaniesHouseFileSetDownloadStatus>(
                DownloadStatusStorage.CompaniesHouseDownloadPartitionKey, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        // Assert
        actual.Should().NotBeNull();
        actual.CurrentRunExpectedFileCount.Should().Be(DownloadStatusStorage.InitialExpectedFileCountSeed);
    }

    [TestMethod]
    public async Task ShouldTryCreateTable()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        // Assert
        tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task ShouldReplyNullGetCompaniesStatusCreateTableFails()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 3, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        tableClient.Setup(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var actual = await downloadStatusStorage.GetCompaniesHouseFileDownloadStatusAsync();

        // Assert
        actual.Should().BeNull();
    }

    [TestMethod]
    public async Task ShouldUploadStatusAndReplySuccess()
    {
        // Arrange
        var status = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        // Act
        var response = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(status);

        // Assert
        response.Should().Be(true);
        tableClient.Verify(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()));
        tableClient.Verify(tc => tc.UpsertEntityAsync<CompaniesHouseFileSetDownloadStatus>(status, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task ShouldReplyFailedWhenUploadStatusCreateTableFails()
    {
        // Arrange
        var status = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new DownloadStatusStorage(tableServiceClient.Object, timeProvider, NullLogger<DownloadStatusStorage>.Instance);

        tableClient.Setup(tc => tc.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("error"));

        // Act
        var response = await downloadStatusStorage.SetCompaniesHouseFileDownloadStatusAsync(status);

        // Assert
        response.Should().Be(false);
    }
}
