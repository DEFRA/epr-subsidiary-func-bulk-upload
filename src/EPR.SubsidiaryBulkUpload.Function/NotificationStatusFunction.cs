using System.Collections.Generic;
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
            var keys = GenerateKeys(userId, organisationId);
            var status = await _notificationService.GetStatus(keys.Progress);
            var rowsAdded = await _notificationService.GetStatus(keys.RowsAdded);
            var errorStatus = await _notificationService.GetStatus(keys.Errors);

            var errors = !string.IsNullOrEmpty(errorStatus)
                ? JsonSerializer.Deserialize<UploadFileErrorCollectionModel>(errorStatus)
                : null;

            if (errors is not null)
            {
                errors.Errors.Sort((e1, e2) => e1.FileLineNumber.CompareTo(e2.FileLineNumber));
            }

            var rowsAddedCount = int.TryParse(rowsAdded, out int parsedRowsAdded) ? parsedRowsAdded : (int?)null;

            return status is null
                ? new NotFoundResult()
                : new JsonResult(new
                {
                    Status = status,
                    RowsAdded = rowsAddedCount,
                    Errors = errors
                });
        }
        catch (Exception ex)
        {
            return new StatusCodeResult(500);
        }
    }

    [Function("NotificationStatusResetFunction")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "notifications/status/{userId}/{organisationId}")]
        HttpRequest req,
        Guid userId,
        Guid organisationId)
    {
        try
        {
            var keys = GenerateKeys(userId, organisationId);
            await _notificationService.ClearRedisKeyAsync(keys.Progress);
            await _notificationService.ClearRedisKeyAsync(keys.RowsAdded);
            await _notificationService.ClearRedisKeyAsync(keys.Errors);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            return new StatusCodeResult(500);
        }
    }

    private static (string? Progress, string? RowsAdded, string? Errors) GenerateKeys(
        Guid userId,
        Guid organisationId)
    {
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        return (userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadProgress),
                userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadRowsAdded),
                userRequestModel.GenerateKey(NotificationStatusKeys.SubsidiaryBulkUploadErrors));
    }
}
