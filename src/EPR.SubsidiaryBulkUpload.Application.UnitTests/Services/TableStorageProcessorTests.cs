using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class TableStorageProcessorTests
{
    private Mock<TableServiceClient> _mockTableServiceClient;
    private Mock<TableClient> _mockTableClient;
    private Mock<ILogger<TableStorageProcessor>> _mockLogger;
    private TableStorageProcessor _processor;
    private Fixture _fixture;

    [TestInitialize]
    public void Setup()
    {
        _mockTableServiceClient = new Mock<TableServiceClient>();
        _mockTableClient = new Mock<TableClient>();
        _mockLogger = new Mock<ILogger<TableStorageProcessor>>();

        _mockTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>()))
            .Returns(_mockTableClient.Object);

        _processor = new TableStorageProcessor(_mockTableServiceClient.Object, _mockLogger.Object);

        _fixture = new Fixture();
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_ShouldProcessRecordsSuccessfully()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new CompanyHouseTableEntity { CompanyNumber = "123" },
                new CompanyHouseTableEntity { CompanyNumber = "456" }
            };

        // Mock CreateIfNotExistsAsync and other calls
        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey", "TestConnectionString", 2);

        // Assert
        _mockTableClient.Verify(x => x.CreateIfNotExistsAsync(default), Times.Once);
        _mockTableClient.Verify(
            x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()),
            Times.Once);
        _mockTableClient.Verify(x => x.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default), Times.Exactly(1));
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Latest"), TableUpdateMode.Merge, default), Times.Once);
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_ShouldDeleteEntityLogErrorAndRethrowException_OnFailure()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new CompanyHouseTableEntity { CompanyNumber = "123" }
            };

        var exception = new Exception("Test Exception");

        _mockTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<CompanyHouseTableEntity>(), TableUpdateMode.Merge, default))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(async () =>
            await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey", "TestConnectionString", 2));

        _mockLogger.Verify(
            x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            exception,
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_ShouldUseDefaultPartitionKey_WhenPartitionKeyIsEmpty()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new CompanyHouseTableEntity { CompanyNumber = "123" }
            };

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<CompanyHouseTableEntity>(), TableUpdateMode.Merge, default))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", string.Empty, "TestConnectionString", 2);

        // Assert
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.PartitionKey == "EmptyPartitionKey" && e.RowKey == "Current Ingestion"), TableUpdateMode.Merge, default), Times.Once);
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage()
    {
        // Arrange
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = new Mock<Response<CompanyHouseTableEntity>>();
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        companyResponse.Setup(x => x.Value).Returns(companyData);
        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCHData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(partitionData.Data, companyData.CompanyNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyResponse.Object);

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, "table");

        // Assert
        actual.Should().BeEquivalentTo(companyData);
    }

    [TestMethod]
    public async Task ShouldNotGetCompaniesWhenCannotGetPartition()
    {
        // Arrange
        var exception = new Exception("error");

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCHData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var actual = await _processor.GetByCompanyNumber("companyNumber", "table");

        // Assert
        actual.Should().BeNull();
    }
}
