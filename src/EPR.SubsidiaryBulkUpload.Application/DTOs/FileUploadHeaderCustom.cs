namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class FileUploadHeaderCustom : FileUploadHeader
{
    public string joiner_date { get; set; }

    public string reporting_type { get; set; }
}