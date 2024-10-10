using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        var fileParts = new List<CompanyHouseTableEntity>
            {
                new() { Data = JsonConvert.SerializeObject(new FilePart(1, 3, "2024-09=-0")) },
                new() { Data = JsonConvert.SerializeObject(new FilePart(2, 3, "2024-09=-0")) },
                new() { Data = JsonConvert.SerializeObject(new FilePart(3, 3, "2024-09=-0")) },
            };
        fileParts = new List<CompanyHouseTableEntity>();

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.SetupSequence(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(fileParts))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(records));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _mockTableClient.Setup(x =>
            x.DeleteEntityAsync(
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
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_ShouldProcessRecordsSuccessfully_With_Multiple_FileParts()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new() { CompanyNumber = "123" },
                new() { CompanyNumber = "456" }
            };

        var fileParts = new List<CompanyHouseTableEntity>
            {
                new() { Data = JsonConvert.SerializeObject(new FilePart(1, 3, "2024-09=-0")) },
                new() { Data = JsonConvert.SerializeObject(new FilePart(2, 3, "2024-09=-0")) },
                new() { Data = JsonConvert.SerializeObject(new FilePart(3, 3, "2024-09=-0")) },
            };

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.SetupSequence(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(fileParts))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(records));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _mockTableClient.Setup(x =>
            x.DeleteEntityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey", 1, 3);

        // Assert
        _mockTableClient.Verify(x => x.CreateIfNotExistsAsync(default), Times.Once);
        _mockTableClient.Verify(
            x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()),
            Times.Once);
        _mockTableClient.Verify(x => x.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default), Times.Exactly(1));
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_Should_Write_Status_Records()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new() { CompanyNumber = "123" },
                new() { CompanyNumber = "456" }
            };

        var transactionResponse = new Mock<Response<IReadOnlyList<Response>>>();
        transactionResponse
            .Setup(x => x.Value)
            .Returns(new List<Response>
            {
                new Mock<Response>().Object,
                new Mock<Response>().Object
            });

        var currentPartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.CurrentIngestion,
            Data = "2024-09-01"
        };
        var latestPartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.ToDelete,
            Data = "2024-08-01"
        };
        var previousPartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.ToDelete,
            Data = "2024-07-01"
        };
        var toDeletePartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.ToDelete,
            Data = "2024-06-01"
        };

        var toDeletePartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        toDeletePartitionResponse.Setup(x => x.Value).Returns(toDeletePartitionData);

        var previousPartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        previousPartitionResponse.Setup(x => x.Value).Returns(previousPartitionData);

        var latestPartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        latestPartitionResponse.Setup(x => x.Value).Returns(latestPartitionData);

        var currentPartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        currentPartitionResponse.Setup(x => x.Value).Returns(currentPartitionData);

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));
        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.CurrentIngestion, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPartitionResponse.Object);
        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestPartitionResponse.Object);
        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Previous, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousPartitionResponse.Object);
        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.ToDelete, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toDeletePartitionResponse.Object);

        _mockTableClient.SetupSequence(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(records))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity>()));

        _mockTableClient.Setup(x =>
            x.DeleteEntityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _mockTableClient
            .Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default))
            .ReturnsAsync(transactionResponse.Object);

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey");

        // Assert
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"), TableUpdateMode.Merge, It.IsAny<CancellationToken>()), Times.Once);
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Previous"), TableUpdateMode.Merge, default), Times.Once);
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Latest"), TableUpdateMode.Merge, default), Times.Once);
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "To Delete"), TableUpdateMode.Merge, default), Times.Once);
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_Should_NotWrite_Status_Records_IfNotAllFilePartsCompleted()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new() { CompanyNumber = "123" },
                new() { CompanyNumber = "456" }
            };

        var fileParts = new List<CompanyHouseTableEntity>
            {
                new() { Data = JsonConvert.SerializeObject(new FilePart(1, 3, "2024-09=-0")) },
                new() { Data = JsonConvert.SerializeObject(new FilePart(2, 3, "2024-09=-0")) },
            };

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.SetupSequence(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(fileParts));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(
            It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Current Ingestion"),
            TableUpdateMode.Merge,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey");

        // Assert
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Previous"), TableUpdateMode.Merge, default), Times.Never);
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "Latest"), TableUpdateMode.Merge, default), Times.Never);
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.RowKey == "To Delete"), TableUpdateMode.Merge, default), Times.Never);
    }

    [TestMethod]
    public async Task WriteToAzureTableStorage_Should_Delete_Old_Data()
    {
        // Arrange
        var records = new List<CompanyHouseTableEntity>
            {
                new() { CompanyNumber = "123" },
                new() { CompanyNumber = "456" }
            };

        var transactionResponse = new Mock<Response<IReadOnlyList<Response>>>();
        transactionResponse
            .Setup(x => x.Value)
            .Returns(new List<Response>
            {
                Mock.Of<Response>(),
                Mock.Of<Response>()
            });

        var currentPartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.CurrentIngestion,
            Data = "2024-09-01"
        };

        var toDeletePartitionData = new CompanyHouseTableEntity
        {
            PartitionKey = TableStorageProcessor.LatestCompaniesHouseData,
            RowKey = TableStorageProcessor.ToDelete,
            Data = "2024-06-01"
        };

        var currentPartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        currentPartitionResponse.Setup(x => x.Value).Returns(currentPartitionData);

        var toDeletePartitionResponse = new Mock<Response<CompanyHouseTableEntity>>();
        toDeletePartitionResponse.Setup(x => x.Value).Returns(toDeletePartitionData);

        _mockTableClient.Setup(x => x.CreateIfNotExistsAsync(default));

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.CurrentIngestion, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPartitionResponse.Object);
        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.ToDelete, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toDeletePartitionResponse.Object);

        _mockTableClient.SetupSequence(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(records))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity>()));

        _mockTableClient.Setup(x => x
            .DeleteEntityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _mockTableClient
            .Setup(tc => tc.SubmitTransactionAsync(It.IsAny<IEnumerable<TableTransactionAction>>(), default))
            .ReturnsAsync(transactionResponse.Object);

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", "TestPartitionKey");

        // Assert
        _mockTableClient.Verify(
            x => x.SubmitTransactionAsync(
                It.Is<IEnumerable<TableTransactionAction>>(
                    t => t.Count() == 2 &&
                         t.ToArray()[0].ActionType == TableTransactionActionType.Delete &&
                         t.ToArray()[1].ActionType == TableTransactionActionType.Delete),
                default),
            Times.Exactly(1));

        _mockTableClient.Verify(x => x.DeleteEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.PartitionKey == "Latest CH Data" && e.RowKey == "To Delete"), It.IsAny<ETag>(), default), Times.Once);
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

        _mockTableClient.Verify(x => x.DeleteEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.PartitionKey == "Latest CH Data" && e.RowKey == "Current Ingestion"), It.IsAny<ETag>(), default), Times.Once);

        _mockLogger.VerifyLog(x => x.LogError(It.IsAny<Exception>(), "An error occurred during ingestion."), Times.Once);
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

        _mockTableClient.Setup(tc =>
           tc.QueryAsync<CompanyHouseTableEntity>(
               It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
               It.IsAny<int?>(),
               It.IsAny<IEnumerable<string>>(),
               It.IsAny<CancellationToken>()))
           .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity>()));

        _mockTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<CompanyHouseTableEntity>(), TableUpdateMode.Merge, default))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _processor.WriteToAzureTableStorage(records, "TestTable", string.Empty);

        // Assert
        _mockTableClient.Verify(x => x.UpsertEntityAsync(It.Is<CompanyHouseTableEntity>(e => e.PartitionKey == "Latest CH Data" && e.RowKey == "Current Ingestion" && e.Data == "EmptyPartitionKey"), TableUpdateMode.Merge, default), Times.Once);
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage()
    {
        // Arrange
        const string tableName = "table";
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = new Mock<Response<CompanyHouseTableEntity>>();
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        companyResponse.Setup(x => x.Value).Returns(companyData);
        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(new List<CompanyHouseTableEntity> { companyData }));

        // Act
        var actual = await _processor.GetByCompanyNumber(partitionData.Data, tableName);

        // Assert
        actual.Should().BeEquivalentTo(companyData);
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage_When_QueryAsync_ReturnsEmptyResult()
    {
        // Arrange
        const string tableName = "table";
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = Enumerable.Empty<CompanyHouseTableEntity>().ToList();
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<CompanyHouseTableEntity>(companyResponse));

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, tableName);

        // Assert
        actual.Should().BeNull();
    }

    [TestMethod]
    public async Task ShouldGetCompaniesFromStorage_When_QueryAsync_ReturnsNull()
    {
        // Arrange
        const string tableName = "table";
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = null as Response<CompanyHouseTableEntity>;
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        partitionResponse.Setup(x => x.Value).Returns(partitionData);

        _mockTableClient.Setup(tc =>
         tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(null as AsyncPageable<CompanyHouseTableEntity>);

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, tableName);

        // Assert
        actual.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCompaniesFromStorage_Should_Return_Null_And_Log_Error_When_QueryAsync_Throws_Exception()
    {
        // Arrange
        const string tableName = "table";
        var companyData = _fixture.Create<CompanyHouseTableEntity>();
        var partitionData = _fixture.Create<CompanyHouseTableEntity>();

        var companyResponse = Enumerable.Empty<CompanyHouseTableEntity>().ToList();
        var partitionResponse = new Mock<Response<CompanyHouseTableEntity>>();

        partitionResponse.Setup(x => x.Value).Returns(partitionData);
        var exception = new Exception("Failed");

        _mockTableClient.Setup(tc =>
         tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partitionResponse.Object);

        _mockTableClient.Setup(tc =>
            tc.QueryAsync<CompanyHouseTableEntity>(
                It.IsAny<Expression<Func<CompanyHouseTableEntity, bool>>>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        // Act
        var actual = await _processor.GetByCompanyNumber(companyData.CompanyNumber, tableName);

        // Assert
        actual.Should().BeNull();

        _mockLogger.VerifyLog(
            x => x.LogError(
                It.IsAny<Exception>(),
                "An error occurred whilst retrieving companies house details."),
            Times.Once);
    }

    [TestMethod]
    public async Task ShouldNotGetCompaniesWhenCannotGetPartition()
    {
        // Arrange
        const string tableName = "table";
        var exception = new Exception("error");

        _mockTableClient.Setup(tc =>
            tc.GetEntityAsync<CompanyHouseTableEntity>(TableStorageProcessor.LatestCompaniesHouseData, TableStorageProcessor.Latest, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var actual = await _processor.GetByCompanyNumber("companyNumber", tableName);

        // Assert
        actual.Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteByPartitionKey_Should_Delete_CompaniesFromStorage()
    {
        // Arrange
        const string tableName = "table";
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
        var actual = await _processor.DeleteByPartitionKey(tableName, partitionKey);

        // Assert
        actual.Should().Be(companyData.Count);
    }

    [TestMethod]
    public async Task DeleteByPartitionKey_Should_Log_RequestFailedException_And_Return_Zero()
    {
        // Arrange
        const string tableName = "table";
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

        // Act
        var actual = await _processor.DeleteByPartitionKey(tableName, partitionKey);

        // Assert
        actual.Should().Be(0);

        _mockLogger.VerifyLog(
            x => x.LogError(
                It.IsAny<RequestFailedException>(),
                "DeleteByPartitionKey: error for table '{TableName}' partition key '{PartitionKey}'. Returning 0 results.",
                tableName,
                partitionKey),
            Times.Once);
    }
}
