using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace EPR.SubsidiaryBulkUpload.Function;

public class NotificationStatusFunction(INotificationService notificationService)
{
    private readonly INotificationService _notificationService = notificationService;

    [Function("NotificationStatusFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications/status/{userId}/{organisationId}")]
        HttpRequest req,
        Guid userId,
        Guid organisationId)
    {
        try
        {
            var userRequestModel = new UserRequestModel
            {
                UserId = userId,
                OrganisationId = organisationId
            };

            var progressKey = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress);
            var rowsAddedKey = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded);
            var errorsKey = userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors);

            var status = await _notificationService.GetStatus(progressKey);
            var rowsAdded = await _notificationService.GetStatus(rowsAddedKey);
            var errorStatus = await _notificationService.GetStatus(errorsKey);

            var errors = !string.IsNullOrEmpty(errorStatus)
                ? JsonSerializer.Deserialize<UploadFileErrorCollectionModel>(errorStatus)
                : null;

            return status is null
                ? new NotFoundResult()
                : new JsonResult(new
                {
                    Status = status,
                    RowsAdded = (int?)(int.TryParse(rowsAdded, out int parsedRowsAdded) ? parsedRowsAdded : null),
                    Errors = errors
                });
        }
        catch (Exception ex)
        {
            return new StatusCodeResult(500);
        }
    }
}
