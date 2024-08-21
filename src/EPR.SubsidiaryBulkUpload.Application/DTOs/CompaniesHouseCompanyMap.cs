using System.Text;
using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        Map(c => c.organisation_id).Name("organisation_id");
        Map(c => c.subsidiary_id).Name("subsidiary_id").TypeConverterOption.NullValues(string.Empty);
        Map(c => c.organisation_name).Name("organisation_name");
        Map(c => c.companies_house_number).Name("companies_house_number");
        Map(c => c.parent_child).Name("parent_child");
        Map(c => c.franchisee_licensee_tenant).Name("franchisee_licensee_tenant");
        Map(c => c.Errors).Convert(args =>
        {
            var errors = new StringBuilder();
            if (string.IsNullOrEmpty(args.Value.organisation_id))
            {
                errors.Append("organisation_id is null");
            }

            if (string.IsNullOrEmpty(args.Value.organisation_name))
            {
                errors.Append("organisation_name is null");
            }

            if (string.IsNullOrEmpty(args.Value.companies_house_number))
            {
                errors.Append("companies_house_number is null");
            }

            if (string.IsNullOrEmpty(args.Value.parent_child))
            {
                errors.Append("parent_child is null");
            }

            return errors.ToString();
        });
/*        Map(c => c.Errors).Convert((IReaderRow row) =>
        {
            var errors = new StringBuilder();
            if (string.IsNullOrEmpty(row.GetField(nameof(c.organisation_id))))
            {
                errors.Append("organisation_id is null");
            }

            return errors.ToString();
        });*/
    }
}
