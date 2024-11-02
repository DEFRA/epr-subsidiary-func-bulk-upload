using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class NotificationService(
    ILogger<NotificationService> logger,
    IConnectionMultiplexer redisConnectionMultiplexer,
    IOptions<RedisOptions> redisOptions) : INotificationService
{
    private static readonly object _padlock = new();

    private readonly ILogger<NotificationService> _logger = logger;
    private readonly IDatabase _redisDatabase = redisConnectionMultiplexer.GetDatabase();
    private readonly RedisOptions _redisOptions = redisOptions.Value;

    private readonly JsonSerializerOptions _caseInsensitiveJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SetStatus(string key, string status)
    {
        var expiry = GetExpiry();
        await _redisDatabase.StringSetAsync(key, status, expiry);

        _logger.LogInformation("Redis updated key: {Key} status: {Status}", key, status);
    }

    public async Task SetErrorStatus(string key, List<UploadFileErrorModel> errorsModel)
    {
        var expiry = GetExpiry();
        List<UploadFileErrorModel> errors;

        lock (_padlock)
        {
            var previousErrors = _redisDatabase.StringGet(key);
            if (previousErrors.IsNullOrEmpty)
            {
                errors = errorsModel;
            }
            else
            {
                var response = JsonSerializer.Deserialize<UploadFileErrorResponse>(previousErrors, _caseInsensitiveJsonSerializerOptions);
                errors = response.Errors;
                errors.AddRange(errorsModel);
            }

            var value = SerializeErrorsToJson(errors);
            _redisDatabase.StringSet(key, value, expiry);

            _logger.LogInformation("Redis updated key: {Key} errors: {Value}", key, value);
        }
    }

    public async Task<string?> GetStatus(string key)
    {
        var value = await _redisDatabase.StringGetAsync(key);
        return value.IsNull
            ? null
            : value.ToString();
    }

    public async Task<UploadFileErrorResponse> GetNotificationErrorsAsync(string key)
    {
        var value = await _redisDatabase.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return new UploadFileErrorResponse { Errors = new() };
        }

        _logger.LogInformation("Redis errors response key: {Key} errors: {Value}", key, value);

        return JsonSerializer.Deserialize<UploadFileErrorResponse>(value, _caseInsensitiveJsonSerializerOptions);
    }

    public async Task ClearRedisKeyAsync(string key)
    {
        var isDeleted = await _redisDatabase.KeyDeleteAsync(key);

        if (isDeleted)
        {
            _logger.LogInformation("Redis key {Key} deleted successfully.", key);
        }
        else
        {
            _logger.LogWarning("Redis key {Key} not found.", key);
        }
    }

    private static string SerializeErrorsToJson(List<UploadFileErrorModel> errors)
    {
        return JsonSerializer.Serialize(new { Errors = errors });
    }

    private TimeSpan? GetExpiry() =>
         _redisOptions.TimeToLiveInMinutes is not null
        ? TimeSpan.FromMinutes(_redisOptions.TimeToLiveInMinutes.Value)
        : default(TimeSpan?);
}
