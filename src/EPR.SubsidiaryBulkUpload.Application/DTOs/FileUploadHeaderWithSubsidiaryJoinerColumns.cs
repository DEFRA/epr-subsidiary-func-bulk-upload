using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;
[ExcludeFromCodeCoverage]

public class FileUploadHeaderWithSubsidiaryJoinerColumns : FileUploadHeader
{
    public string joiner_date { get; set; }

    public string reporting_type { get; set; }
}