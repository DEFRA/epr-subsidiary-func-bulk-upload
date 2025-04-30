using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class FileUploadHeaderWithSubsidiaryJoinerAndNationCodeColumns : FileUploadHeaderWithSubsidiaryJoinerColumns
{
    public string nation_code { get; set; }
}