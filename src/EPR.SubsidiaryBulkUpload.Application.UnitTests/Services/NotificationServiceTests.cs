using System.Text.Json;
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
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {key} status: {status}", key, status), Times.Once);
        ////_redisConnectionMultiplexerMock.Verify(x => x.Close(true), Times.Once);
    }

    [TestMethod]
    public async Task SetErrorStatus_ShouldSerializeErrorsAndSetStringInRedisAndLogInformation()
    {
        // Arrange
        var key = "testKey";
        var errorsModel = new List<string> { "Error1", "Error2" };
        var serializedErrors = JsonSerializer.Serialize(new { Errors = errorsModel });

        _redisDatabaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .ReturnsAsync(true);

        // Act
        await _notificationService.SetErrorStatus(key, errorsModel);

        // Assert
        _redisDatabaseMock.Verify(db => db.StringSetAsync(key, serializedErrors, null, false, When.Always, CommandFlags.None), Times.Once);
        _loggerMock.VerifyLog(x => x.LogInformation("Redis updated key: {key} errors: {value}", key, serializedErrors), Times.Once);
        ////_redisConnectionMultiplexerMock.Verify(x => x.Close(true), Times.Once);
    }
}
