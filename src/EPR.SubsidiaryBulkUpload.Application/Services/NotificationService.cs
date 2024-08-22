using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class NotificationService(
    ILogger<NotificationService> logger, IConnectionMultiplexer redisConnectionMultiplexer) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly IDatabase _redisDatabase = redisConnectionMultiplexer.GetDatabase();

    public async Task SetStatus(string key, string status)
    {
        await _redisDatabase.StringSetAsync(key, status);

        _logger.LogInformation("Redis updated key: {key} status: {status}", key, status);
    }

    public async Task SetErrorStatus<T>(string key, List<T> errorsModel)
    {
        var value = SerializeErrorsToJson(errorsModel);

        await _redisDatabase.StringSetAsync(key, value);

        _logger.LogInformation("Redis updated key: {key} errors: {value}", key, value);
    }

    private string SerializeErrorsToJson<T>(List<T> errors)
    {
        return JsonSerializer.Serialize(new { Errors = errors });
    }
}
