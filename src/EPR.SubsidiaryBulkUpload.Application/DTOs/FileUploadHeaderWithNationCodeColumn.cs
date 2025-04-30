using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class FileUploadHeaderWithNationCodeColumn : FileUploadHeader
{
    public string nation_code { get; set; }
}