using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface INotificationService
{
    Task<string?> GetStatus(string key);

    Task SetStatus(string key, string status);

    Task SetErrorStatus(string key, List<UploadFileErrorModel> errorsModel);
}
