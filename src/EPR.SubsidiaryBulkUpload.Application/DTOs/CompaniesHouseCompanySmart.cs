using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompanySmart
{
    public string organisation_id { get; set; }

    public string subsidiary_id { get; set; }

    public string organisation_name { get; set; }

    public string companies_house_number { get; set; }

    public string parent_child { get; set; }

    public string franchisee_licensee_tenant { get; set; }
}