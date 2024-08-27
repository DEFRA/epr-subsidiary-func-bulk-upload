using System.Text;
using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        Map(m => m.organisation_id).Validate(field => !field.Equals(null));
        Map(m => m.subsidiary_id);
        Map(m => m.organisation_name).Validate(field => !field.Equals(string.Empty));
        Map(m => m.companies_house_number).Validate(field => !field.Equals(string.Empty));
        Map(m => m.parent_child).Validate(field => !field.Equals(string.Empty));
        Map(m => m.franchisee_licensee_tenant);
        Map(m => m.Errors).Convert(args =>
        {
            var theRow = args.Row;
            var errors = new StringBuilder();
            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.organisation_id))))
            {
                errors.Append("Organisation_id is null");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.subsidiary_id))))
            {
                errors.Append("Sub Org_id is null");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.organisation_name))))
            {
                errors.Append("Organisation_name is null");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.companies_house_number))))
            {
                errors.Append("Organisation_number is null");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.parent_child))))
            {
                errors.Append("parent_or_child is null");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))))
            {
                errors.Append("License is null");
            }

            return errors.ToString();
        });
    }
}