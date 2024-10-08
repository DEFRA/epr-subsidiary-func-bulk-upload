﻿using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class NotificationService(
    ILogger<NotificationService> logger, IConnectionMultiplexer redisConnectionMultiplexer) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly IDatabase _redisDatabase = redisConnectionMultiplexer.GetDatabase();

    private readonly JsonSerializerOptions _caseInsensitiveJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SetStatus(string key, string status)
    {
        await _redisDatabase.StringSetAsync(key, status);

        _logger.LogInformation("Redis updated key: {Key} status: {Status}", key, status);
    }

    public async Task SetErrorStatus(string key, List<UploadFileErrorModel> errorsModel)
    {
        var previousErrors = await GetNotificationErrorsAsync(key);
        previousErrors.Errors.AddRange(errorsModel);

        var value = SerializeErrorsToJson(previousErrors.Errors);

        await _redisDatabase.StringSetAsync(key, value);

        _logger.LogInformation("Redis updated key: {Key} errors: {Value}", key, value);
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
}
