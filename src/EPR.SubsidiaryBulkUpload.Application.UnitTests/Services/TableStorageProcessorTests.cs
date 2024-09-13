using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;
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
    public void TestInitialize()
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
                new() { CompanyNumber = "123" },
                new() { CompanyNumber = "456" }
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
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey");

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
                new() { CompanyNumber = "123" }
            };

        var exception = new Exception("Test Exception");

        _mockTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<CompanyHouseTableEntity>(), TableUpdateMode.Merge, default))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(async () =>
            await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey"));

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
                new() { CompanyNumber = "123" }
            };

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<CompanyHouseTableEntity>(), TableUpdateMode.Merge, default))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", string.Empty);

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
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity> { companyData }));

        // Act
        var actual = await _processor.GetByCompanyNumber("table", partitionData.Data);

        // Assert
        actual.Should().BeEquivalentTo(companyData);
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage_When_QueryAsync_ReturnsEmptyResult()
    {
        // Arrange
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = Enumerable.Empty<CompanyHouseTableEntity>().ToList();
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCHData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(companyResponse));

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, "table");

        // Assert
        actual.Should().BeNull();
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage_When_QueryAsync_ReturnsNull()
    {
        // Arrange
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = null as Response<CompanyHouseTableEntity>;
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCHData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(null as AsyncPageable<CompanyHouseTableEntity>);

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, "table");

        // Assert
        actual.Should().BeNull();
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

    [TestMethod]
    public async Task DeleteObsoleteRecords_Should_Return_Count()
    {
        // Arrange

        // Act
        var result = await _processor.DeleteObsoleteRecords("table");

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task DeleteByPartitionKey_Should_Delete_CompaniesFromStorage()
    {
        // Arrange
        const string partitionKey = "TestPartitionKey";
        var companyData = _fixture
            .Build<CompanyHouseTableEntity>()
            .With(c => c.PartitionKey, () => partitionKey)
            .CreateMany(3)
            .ToList();

        var transactionResponse = new Mock<Response<IReadOnlyList<Response>>>();
        transactionResponse
            .Setup(x => x.Value)
            .Returns(new List<Response>
            {
                new Mock<Response>().Object,
                new Mock<Response>().Object,
                new Mock<Response>().Object
            });

        _mockTableClient
            .Setup(tc => tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(companyData));

        _mockTableClient
            .Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default))
            .ReturnsAsync(transactionResponse.Object);

        // Act
        var actual = await _processor.DeleteByPartitionKey(partitionKey, "table");

        // Assert
        actual.Should().Be(companyData.Count);

        _mockLogger.VerifyLog(x => x.LogError(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteByPartitionKey_Should_Log_RequestFailedException_And_Return_Zero()
    {
        // Arrange
        const string partitionKey = "TestPartitionKey";
        var exception = new RequestFailedException("Request failed");

        var companyData = _fixture
            .Build<CompanyHouseTableEntity>()
            .With(c => c.PartitionKey, () => partitionKey)
            .CreateMany(3)
            .ToList();

        _mockTableClient
            .Setup(tc => tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(companyData));

        _mockTableClient
            .Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default))
            .Throws(exception);

        // Act & Assert
        // Act
        var actual = await _processor.DeleteByPartitionKey(partitionKey, "table");

        // Assert
        actual.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);

        _mockLogger.VerifyLog(x => x.LogError(It.IsAny<string>()), Times.Once);
    }
}
