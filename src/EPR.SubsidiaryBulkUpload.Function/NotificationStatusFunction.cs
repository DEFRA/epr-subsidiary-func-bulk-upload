using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace EPR.SubsidiaryBulkUpload.Function;

public class NotificationStatusFunction(INotificationService notificationService)
{
    private const string SubsidiaryBulkUploadProgress = "Subsidiary bulk upload progress";
    private readonly INotificationService _notificationService = notificationService;

    [Function("NotificationStatusFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications/status/{userId}/{organisationId}")]
        HttpRequest req,
        Guid userId,
        Guid organisationId)
    {
        var key = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        }
        .GenerateKey(SubsidiaryBulkUploadProgress);

        var value = await _notificationService.GetStatus(key);
        return value is null
            ? new NotFoundResult()
            : new OkObjectResult(value.ToString());
    }
}
