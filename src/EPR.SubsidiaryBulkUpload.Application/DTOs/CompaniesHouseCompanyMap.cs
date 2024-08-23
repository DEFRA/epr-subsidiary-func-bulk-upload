using System.Text;
using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap(StringBuilder logger)
    {
        if (logger == null)
        {
            logger = new StringBuilder();
        }

        Map(c => c.organisation_id).Name("organisation_id").Validate(args =>
        {
            var isValid = !string.IsNullOrEmpty(args.Field);
            if (!isValid)
            {
                logger.AppendLine($"Field '{args.Field}' is not valid!");
                return false;
            }

            var isNumeric = int.TryParse(args.Field, out int n);
            if (!isNumeric)
            {
                logger.AppendLine($"Field '{args.Field}' is not number!");
                return false;
            }

            return true;
        });
        Map(c => c.subsidiary_id).Name("subsidiary_id").TypeConverterOption.NullValues(string.Empty);
        /*Map(c => c.organisation_name).Name("organisation_name").Validate(args => !string.IsNullOrEmpty(args.Field));*/
        Map(c => c.organisation_name).Name("organisation_name").Validate(args =>
        {
            var isValid = !string.IsNullOrEmpty(args.Field);
            if (!isValid)
            {
                logger.AppendLine($"Field '{args.Field}' is not valid!");
                return false;
            }

            return true;
        });
        Map(c => c.companies_house_number).Name("companies_house_number").Validate(args =>
        {
            var isValid = !string.IsNullOrEmpty(args.Field);
            if (!isValid)
            {
                logger.AppendLine($"Field '{args.Field}' is not valid!");
                return false;
            }

            return true;
        });
        Map(c => c.parent_child).Name("parent_child").Validate(args =>
        {
            var isValid = !string.IsNullOrEmpty(args.Field);
            if (!isValid)
            {
                logger.AppendLine($"Field '{args.Field}' is not valid!");
                return false;
            }

            return true;
        });
        Map(c => c.franchisee_licensee_tenant).Name("franchisee_licensee_tenant");
        Map(c => c.Errors).Convert(args =>
        {
            var errors = new StringBuilder();
            if (string.IsNullOrEmpty(args.Value.organisation_id))
            {
                logger.Append("organisation_id is null");
            }

            if (string.IsNullOrEmpty(args.Value.organisation_name))
            {
                logger.Append("organisation_name is null");
            }

            if (string.IsNullOrEmpty(args.Value.companies_house_number))
            {
                logger.Append("companies_house_number is null");
            }

            if (string.IsNullOrEmpty(args.Value.parent_child))
            {
                logger.Append("parent_child is null");
            }

            return logger.ToString();
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
