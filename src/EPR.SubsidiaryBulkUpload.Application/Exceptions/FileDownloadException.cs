using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Exceptions;

[ExcludeFromCodeCoverage(Justification = "Exceptions are not tested; Their use is confirmed in service tests")]
public class FileDownloadException : Exception
{
    public FileDownloadException()
    {
    }

    public FileDownloadException(string message)
        : base(message)
    {
    }

    public FileDownloadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
