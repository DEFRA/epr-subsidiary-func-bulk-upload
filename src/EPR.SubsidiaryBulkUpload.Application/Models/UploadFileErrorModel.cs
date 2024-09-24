using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UploadFileErrorModel
{
    public int FileLineNumber { get; set; }

    public string FileContent { get; set; }

    public string Message { get; set; }

    public int ErrorNumber { get; set; }

    public bool IsError { get; set; }
}
