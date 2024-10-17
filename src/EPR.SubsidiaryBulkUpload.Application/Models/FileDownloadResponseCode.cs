namespace EPR.SubsidiaryBulkUpload.Application.Models;

public enum FileDownloadResponseCode
{
    Succeeded,
    InvalidFilePathUrl,
    FailedToFindFile,
    DownloadTimedOut,
    DownloadCancelled,
    UploadFailed
}
