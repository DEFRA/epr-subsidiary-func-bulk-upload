using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UploadFileErrorCollectionModel
{
    public List<UploadFileErrorModel> Errors { get; set; }
}
