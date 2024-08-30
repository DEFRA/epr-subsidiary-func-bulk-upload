using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
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
    public void Setup()
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

        // _redisDatabaseMock.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == key), It.IsAny<RedisValue>(), It.Is<TimeSpan>(t => t.), It.IsAny<When>())).ReturnsAsync(status);
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
}
