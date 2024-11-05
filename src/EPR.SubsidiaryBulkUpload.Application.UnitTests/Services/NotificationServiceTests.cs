using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using OptionsExtensions = Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class NotificationServiceTests
{
    private const string TestKey = "testKey";
    private const string EmptyKey = "emptyKey";
    private const string MissingKey = "missingKey";
    private const string TestStatus = "testStatus";

    private Mock<ILogger<NotificationService>> _loggerMock;
    private Mock<IConnectionMultiplexer> _redisConnectionMultiplexerMock;
    private Mock<IDatabase> _redisDatabaseMock;
    private RedisOptions _redisOptions;
    private NotificationService _notificationService;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _redisConnectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();

        _redisOptions = new RedisOptions
        {
            ConnectionString = "localhost",
            TimeToLiveInMinutes = 5
        };

        _redisConnectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _notificationService = new NotificationService(
            _loggerMock.Object,
            _redisConnectionMultiplexerMock.Object,
            OptionsExtensions.Options.Create<RedisOptions>(_redisOptions));
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnValueFromRedis()
    {
        // Arrange
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == TestKey), It.IsAny<CommandFlags>())).ReturnsAsync(TestStatus);

        // Act
        var result = await _notificationService.GetStatus(TestKey);

        // Assert
        result.Should().Be(TestStatus);
        _redisDatabaseMock.Verify(db => db.StringGetAsync(TestKey, CommandFlags.None), Times.Once);
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnNullFromRedis_WhenKeyIsMissing()
    {
        // Arrange
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == TestKey), It.IsAny<CommandFlags>())).ReturnsAsync(TestStatus);

        // Act
        var result = await _notificationService.GetStatus(MissingKey);

        // Assert
        result.Should().BeNull();
        _redisDatabaseMock.Verify(db => db.StringGetAsync(MissingKey, CommandFlags.None), Times.Once);
    }

    [TestMethod]
    public async Task SetStatus_ShouldSetStringInRedisAndLogInformation()
    {
        // Arrange
        _redisDatabaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>())).ReturnsAsync(true);

        // Act
        await _notificationService.SetStatus(TestKey, TestStatus);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(TestKey, TestStatus, It.Is<TimeSpan>(t => (int)t.TotalMinutes == _redisOptions.TimeToLiveInMinutes), false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} status: {Status}", TestKey, TestStatus), Times.Once);
    }

    [TestMethod]
    public async Task InitializeUploadStatusAsync_Calls_Redis_When_Ttl_Is_Null()
    {
        // Arrange
        _redisOptions = new RedisOptions
        {
            ConnectionString = "localhost",
            TimeToLiveInMinutes = null
        };

        _redisDatabaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>())).ReturnsAsync(true);

        _notificationService = new NotificationService(
            _loggerMock.Object,
            _redisConnectionMultiplexerMock.Object,
            OptionsExtensions.Options.Create<RedisOptions>(_redisOptions));

        // Act
        await _notificationService.SetStatus(TestKey, TestStatus);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(TestKey, TestStatus, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} status: {Status}", TestKey, TestStatus), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeErrorsAndSetStringInRedisAndLogInformation()
    {
        // Arrange
        var errorsModel = new List<UploadFileErrorModel> { new() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        var serializedErrors = JsonSerializer.Serialize(new { Errors = errorsModel });

        _redisDatabaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .ReturnsAsync(true);

        // Act
        await _notificationService.SetErrorStatus(TestKey, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSet(TestKey, serializedErrors, It.Is<TimeSpan>(t => (int)t.TotalMinutes == _redisOptions.TimeToLiveInMinutes), false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} errors: {Value}", TestKey, serializedErrors), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeCombinedErrorsAndSetStringInRedisAndLogInformation()
    {
        // Arrange
        var previousErrors = new UploadFileErrorResponse
        {
            Errors = new List<UploadFileErrorModel>
                {
                    new() { FileLineNumber = 1, FileContent = "Content1", Message = "Message1", IsError = true, ErrorNumber = 6 },
                    new() { FileLineNumber = 2, FileContent = "Content2", Message = "Message2", IsError = false, ErrorNumber = 9 }
                }
        };
        var errorsModel = new List<UploadFileErrorModel> { new() { FileLineNumber = 1, Message = "testMessage", IsError = true } };

        _redisDatabaseMock.Setup(db => db.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .Returns(true);

        var json = JsonSerializer.Serialize(previousErrors);
        _redisDatabaseMock.Setup(db => db.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns((RedisValue)json);

        previousErrors.Errors.AddRange(errorsModel);
        var serializedErrors = JsonSerializer.Serialize(new { Errors = previousErrors.Errors });

        // Act
        await _notificationService.SetErrorStatus(TestKey, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSet(TestKey, serializedErrors, It.Is<TimeSpan>(t => (int)t.TotalMinutes == _redisOptions.TimeToLiveInMinutes), false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} errors: {Value}", TestKey, serializedErrors), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeErrorsAndSetStringInRedisAndLogInformation_When_Ttl_Is_Null()
    {
        // Arrange
        _redisOptions = new RedisOptions
        {
            ConnectionString = "localhost",
            TimeToLiveInMinutes = null
        };

        var errorsModel = new List<UploadFileErrorModel> { new() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        var serializedErrors = JsonSerializer.Serialize(new { Errors = errorsModel });

        _redisDatabaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>())).ReturnsAsync(true);

        _notificationService = new NotificationService(
            _loggerMock.Object,
            _redisConnectionMultiplexerMock.Object,
            OptionsExtensions.Options.Create<RedisOptions>(_redisOptions));

        // Act
        await _notificationService.SetErrorStatus(TestKey, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSet(TestKey, serializedErrors, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} errors: {Value}", TestKey, serializedErrors), Times.Once);
    }

    [TestMethod]
    public async Task GetNotificationErrorsAsync_EmptyKey_ReturnsEmptyResponse()
    {
        // Arrange
        _redisDatabaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _notificationService.GetNotificationErrorsAsync(EmptyKey);

        // Assert
        result.Should().NotBeNull();
        result.Errors.Should().NotBeNull();
        result.Errors.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetNotificationErrorsAsync_ValidKey_ReturnsErrorsResponse()
    {
        // Arrange
        var errors = new UploadFileErrorResponse
        {
            Errors = new List<UploadFileErrorModel>
                {
                    new() { FileLineNumber = 1, FileContent = "Content1", Message = "Message1", IsError = true, ErrorNumber = 6 },
                    new() { FileLineNumber = 2, FileContent = "Content2", Message = "Message2", IsError = false, ErrorNumber = 9 }
                }
        };

        var json = JsonSerializer.Serialize(errors);
        _redisDatabaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        // Act
        var result = await _notificationService.GetNotificationErrorsAsync(TestKey);

        // Assert
        result.Should().NotBeNull();
        result.Errors.Count.Should().Be(2);
        result.Errors[0].FileLineNumber.Should().Be(1);
        result.Errors[0].FileContent.Should().Be("Content1");
        result.Errors[0].Message.Should().Be("Message1");
        result.Errors[0].ErrorNumber.Should().Be(6);
        result.Errors[0].IsError.Should().BeTrue();
        result.Errors[1].FileLineNumber.Should().Be(2);
        result.Errors[1].FileContent.Should().Be("Content2");
        result.Errors[1].Message.Should().Be("Message2");
        result.Errors[1].IsError.Should().BeFalse();
        result.Errors[1].ErrorNumber.Should().Be(9);

        _loggerMock.VerifyLog(x => x.LogInformation("Redis errors response key: {Key} errors: {Value}", TestKey, json), Times.Once);
    }

    [TestMethod]
    public async Task ClearRedisKeyAsync_KeyExists_ShouldLogKeyDeleted()
    {
        // Arrange
        _redisDatabaseMock
            .Setup(db => db.KeyDeleteAsync(TestKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _notificationService.ClearRedisKeyAsync(TestKey);

        // Assert
        _redisDatabaseMock.Verify(db => db.KeyDeleteAsync(TestKey, It.IsAny<CommandFlags>()), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis key {Key} deleted successfully.", TestKey), Times.Once);
    }

    [TestMethod]
    public async Task ClearRedisKeyAsync_KeyDoesNotExist_ShouldLogKeyNotFound()
    {
        // Arrange
        _redisDatabaseMock
            .Setup(db => db.KeyDeleteAsync(TestKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        await _notificationService.ClearRedisKeyAsync(TestKey);

        // Assert
        _redisDatabaseMock.Verify(db => db.KeyDeleteAsync(TestKey, It.IsAny<CommandFlags>()), Times.Once);
        _loggerMock.VerifyLog(x => x.LogWarning("Redis key {Key} not found.", TestKey), Times.Once);
    }
}
