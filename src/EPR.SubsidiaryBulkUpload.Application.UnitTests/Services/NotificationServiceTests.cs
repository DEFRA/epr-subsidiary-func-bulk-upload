using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using FluentAssertions.Common;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class NotificationServiceTests
{
    private Mock<ILogger<NotificationService>> _loggerMock;
    private Mock<IConnectionMultiplexer> _redisConnectionMultiplexerMock;
    private Mock<IDatabase> _redisDatabaseMock;
    private NotificationService _notificationService;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _redisConnectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();

        _redisConnectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _notificationService = new NotificationService(
            _loggerMock.Object,
            _redisConnectionMultiplexerMock.Object);
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnValueFromRedis()
    {
        // Arrange
        var key = "testKey";
        var status = "testStatus";

        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == key), It.IsAny<CommandFlags>())).ReturnsAsync(status);

        // Act
        var result = await _notificationService.GetStatus(key);

        // Assert
        result.Should().Be(status);
        _redisDatabaseMock.Verify(db => db.StringGetAsync(key, CommandFlags.None), Times.Once);
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnNullFromRedis_WhenKeyIsMissing()
    {
        // Arrange
        var key = "testKey";
        var missingKey = "missingKey";
        var status = "testStatus";

        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == key), It.IsAny<CommandFlags>())).ReturnsAsync(status);

        // Act
        var result = await _notificationService.GetStatus(missingKey);

        // Assert
        result.Should().BeNull();
        _redisDatabaseMock.Verify(db => db.StringGetAsync(missingKey, CommandFlags.None), Times.Once);
    }

    [TestMethod]
    public async Task SetStatus_ShouldSetStringInRedisAndLogInformation()
    {
        // Arrange
        var key = "testKey";
        var status = "testStatus";

        _redisDatabaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>())).ReturnsAsync(true);

        // Act
        await _notificationService.SetStatus(key, status);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(key, status, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} status: {Status}", key, status), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeErrorsAndSetStringInRedisAndLogInformation()
    {
        // Arrange
        var key = "testKey";
        var errorsModel = new List<UploadFileErrorModel> { new UploadFileErrorModel() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        var serializedErrors = JsonSerializer.Serialize(new { Errors = errorsModel });

        _redisDatabaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .ReturnsAsync(true);

        // Act
        await _notificationService.SetErrorStatus(key, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(key, serializedErrors, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} errors: {Value}", key, serializedErrors), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeCombinedErrorsAndSetStringInRedisAndLogInformation()
    {
        // Arrange
        var key = "testKey";

        var previousErrors = new UploadFileErrorResponse
        {
            Errors = new List<UploadFileErrorModel>
                {
                    new UploadFileErrorModel { FileLineNumber = 1, FileContent = "Content1", Message = "Message1", IsError = true, ErrorNumber = 6 },
                    new UploadFileErrorModel { FileLineNumber = 2, FileContent = "Content2", Message = "Message2", IsError = false, ErrorNumber = 9 }
                }
        };
        var errorsModel = new List<UploadFileErrorModel> { new UploadFileErrorModel() { FileLineNumber = 1, Message = "testMessage", IsError = true } };

        _redisDatabaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .ReturnsAsync(true);

        var json = JsonSerializer.Serialize(previousErrors);
        _redisDatabaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        previousErrors.Errors.AddRange(errorsModel);
        var serializedErrors = JsonSerializer.Serialize(new { Errors = previousErrors.Errors });

        // Act
        await _notificationService.SetErrorStatus(key, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(key, serializedErrors, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {Key} errors: {Value}", key, serializedErrors), Times.Once);
    }

    [TestMethod]
    public async Task GetNotificationErrorsAsync_EmptyKey_ReturnsEmptyResponse()
    {
        // Arrange
        var key = "emptyKey";

        _redisDatabaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _notificationService.GetNotificationErrorsAsync(key);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(result.Errors.Count, 0);
    }

    [TestMethod]
    public async Task GetNotificationErrorsAsync_ValidKey_ReturnsErrorsResponse()
    {
        // Arrange
        var key = "validKey";
        var errors = new UploadFileErrorResponse
        {
            Errors = new List<UploadFileErrorModel>
                {
                    new UploadFileErrorModel { FileLineNumber = 1, FileContent = "Content1", Message = "Message1", IsError = true, ErrorNumber = 6 },
                    new UploadFileErrorModel { FileLineNumber = 2, FileContent = "Content2", Message = "Message2", IsError = false, ErrorNumber = 9 }
                }
        };

        var json = JsonSerializer.Serialize(errors);
        _redisDatabaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        // Act
        var result = await _notificationService.GetNotificationErrorsAsync(key);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Errors.Count);
        Assert.AreEqual(1, result.Errors[0].FileLineNumber);
        Assert.AreEqual("Content1", result.Errors[0].FileContent);
        Assert.AreEqual("Message1", result.Errors[0].Message);
        Assert.AreEqual(6, result.Errors[0].ErrorNumber);
        Assert.IsTrue(result.Errors[0].IsError);
        Assert.AreEqual(2, result.Errors[1].FileLineNumber);
        Assert.AreEqual("Content2", result.Errors[1].FileContent);
        Assert.AreEqual("Message2", result.Errors[1].Message);
        Assert.IsFalse(result.Errors[1].IsError);
        Assert.AreEqual(9, result.Errors[1].ErrorNumber);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"Redis errors response key: {key} errors: {json}"),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [TestMethod]
    public async Task ClearRedisKeyAsync_KeyExists_ShouldLogKeyDeleted()
    {
        // Arrange
        var key = "testKey";
        _redisDatabaseMock
            .Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _notificationService.ClearRedisKeyAsync(key);

        // Assert
        _redisDatabaseMock.Verify(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Redis key testKey deleted successfully.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ClearRedisKeyAsync_KeyDoesNotExist_ShouldLogKeyNotFound()
    {
        // Arrange
        var key = "testKey";
        _redisDatabaseMock
            .Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        await _notificationService.ClearRedisKeyAsync(key);

        // Assert
        _redisDatabaseMock.Verify(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Redis key testKey not found.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
