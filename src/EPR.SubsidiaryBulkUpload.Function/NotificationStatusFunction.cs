using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EPR.SubsidiaryBulkUpload.Function;

[ExcludeFromCodeCoverage]
public class NotificationStatusFunction(
    IConnectionMultiplexer redisConnectionMultiplexer,
    ILogger<NotificationStatusFunction> logger)
{
    private const string SubsidiaryBulkUploadProgress = "Subsidiary bulk upload progress";
    private readonly ILogger<NotificationStatusFunction> _logger = logger;
    private readonly IDatabase _redisDatabase = redisConnectionMultiplexer.GetDatabase();

    [Function("NotificationStatusFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications/status/{organisationId}/{userId}")]
        HttpRequest req,
        Guid organisationId,
        Guid userId)
    {
        _logger.LogInformation("Notification status for organisation {OrganisationId} user {UserId}.", organisationId, userId);

        var key = new UserRequestModel { OrganisationId = organisationId, UserId = userId }.GenerateKey(SubsidiaryBulkUploadProgress);

        _logger.LogInformation("Notification key is '{Key}'.", key);

        var value = await _redisDatabase.StringGetAsync(key);
        if (value.IsNull)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(value.ToString());
    }
}
