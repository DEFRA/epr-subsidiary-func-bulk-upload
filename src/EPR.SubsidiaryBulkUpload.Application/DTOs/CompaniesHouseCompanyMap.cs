using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        Map(c => c.Organisation_Id).Index(0);
        Map(c => c.Subsidiary_Id).Index(1);
        Map(c => c.Organisation_Name).Index(2);
        Map(c => c.Companies_House_Number).Index(3);
        Map(c => c.Parent_child).Index(4);
        Map(c => c.Franchisee_licensee_tenant).Index(5);
    }
}