namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface INotificationService
{
    Task SetStatus(string key, string status);

    Task SetErrorStatus<T>(string key, List<T> errorsModel);
}
