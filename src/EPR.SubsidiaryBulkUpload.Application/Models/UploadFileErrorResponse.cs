using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UploadFileErrorResponse
{
    public List<UploadFileErrorModel> Errors { get; set; }
}