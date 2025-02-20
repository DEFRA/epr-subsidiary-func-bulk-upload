namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class FileUploadHeaderCustom
{
    public string organisation_id { get; set; }

    public string subsidiary_id { get; set; }

    public string organisation_name { get; set; }

    public string companies_house_number { get; set; }

    public string parent_child { get; set; }

    public string franchisee_licensee_tenant { get; set; }

    public string joiner_date { get; set; }

    public string reporting_type { get; set; }
}