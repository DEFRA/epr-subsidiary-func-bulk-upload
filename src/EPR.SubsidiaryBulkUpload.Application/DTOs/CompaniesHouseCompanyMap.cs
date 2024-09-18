using System.Diagnostics.CodeAnalysis;
using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        var errorsInRow = string.Empty;

        Map(m => m.organisation_id).Index(0).Validate(field => !field.Equals(null));
        Map(m => m.subsidiary_id);
        Map(m => m.organisation_name).Validate(field => !field.Equals(string.Empty));
        Map(m => m.companies_house_number).Validate(field => !field.Equals(string.Empty));
        Map(m => m.parent_child).Validate(field => !field.Equals(string.Empty));
        Map(m => m.franchisee_licensee_tenant);
        Map(m => m.Errors).Index(6).Convert(args =>
        {
            var theRow = args.Row;
            var errors = new StringBuilder();

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.organisation_id))))
            {
                errors.Append("organisation_id is required.");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.organisation_name))))
            {
                errors.Append("organisation_name is required.");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.companies_house_number))) && string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))))
            {
                errors.Append("companies_house_number is required.");
            }

            if (string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.parent_child))))
            {
                errors.Append("parent_or_child is required.");
            }

            if (!string.IsNullOrEmpty(theRow.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))))
            {
                var franchiseeVal = theRow.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant)).ToLower();
                if (franchiseeVal != "yes" && franchiseeVal != "y")
                {
                    errors.Append("franchisee_licensee_tenant can only be blank or Yes or Y.");
                }
            }

            errorsInRow = errors.ToString();
            return errorsInRow;
        });

        Map(m => m.UploadFileErrorModel).Convert(args =>
        {
            if (string.IsNullOrEmpty(errorsInRow))
            {
                return null;
            }

            var rowErrors = new UploadFileErrorModel
            {
                FileLineNumber = args.Row.Context.Reader.Parser.Row,
                FileContent = args.Row.Context.Reader.Parser.RawRecord,
                Message = errorsInRow,
                IsError = true
            };

            return rowErrors;
        });
    }
}