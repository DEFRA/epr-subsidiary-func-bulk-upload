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

    public async Task SetStatus(string key, string status)
    {
        await _redisDatabase.StringSetAsync(key, status);

        _logger.LogInformation("Redis updated key: {key} status: {status}", key, status);
    }

    public async Task SetErrorStatus(string key, List<UploadFileErrorModel> errorsModel)
    {
        var value = SerializeErrorsToJson(errorsModel);

        await _redisDatabase.StringSetAsync(key, value);

        _logger.LogInformation("Redis updated key: {key} errors: {value}", key, value);
    }

    private string SerializeErrorsToJson(List<UploadFileErrorModel> errors)
    {
        return JsonSerializer.Serialize(new { Errors = errors });
    }
}