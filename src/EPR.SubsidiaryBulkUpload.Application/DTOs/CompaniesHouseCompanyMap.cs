using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        Map(c => c.organisation_id).Name("organisation_id");
        Map(c => c.subsidiary_id).Name("subsidiary_id");
        Map(c => c.organisation_name).Name("organisation_name");
        Map(c => c.companies_house_number).Name("companies_house_number");
        Map(c => c.parent_child).Name("parent_child");
        Map(c => c.franchisee_licensee_tenant).Name("franchisee_licensee_tenant");
    }
}