using System.Diagnostics.CodeAnalysis;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.ClassMaps;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    public CompaniesHouseCompanyMap()
    {
        Map(m => m.organisation_id).Index(0).Validate(field => !field.Equals(null));
        Map(m => m.subsidiary_id);
        Map(m => m.organisation_name).Validate(field => !field.Equals(string.Empty));
        Map(m => m.companies_house_number).Validate(field => !field.Equals(string.Empty));
        Map(m => m.parent_child).Validate(field => !field.Equals(string.Empty));
        Map(m => m.franchisee_licensee_tenant);
        Map(m => m.Errors).Index(6).Convert(args => GetRowValidationErrors(args.Row));
        Map(m => m.RawRow).Convert(args => args.Row.Context.Reader.Parser.RawRecord);
        Map(m => m.FileLineNumber).Convert(args => args.Row.Context.Reader.Parser.Row);
    }

    private static List<UploadFileErrorModel> GetRowValidationErrors(IReaderRow row)
    {
        var errors = new List<UploadFileErrorModel>();

        var lineNumber = row.Context.Reader.Parser.Row;
        var rawData = row.Context.Reader.Parser.RawRecord;

        if (string.IsNullOrWhiteSpace(rawData))
        {
            return errors;
        }

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.organisation_id))))
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.OrganisationIdRequiredMessage, BulkUpdateErrors.OrganisationIdRequired));
        }

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.organisation_name))))
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.OrganisationNameRequiredMessage, BulkUpdateErrors.OrganisationNameRequired));
        }

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.companies_house_number))) && string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))))
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.CompaniesHouseNumberRequiredMessage, BulkUpdateErrors.CompaniesHouseNumberRequired));
        }

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.companies_house_number))) && !string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))) && row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant)) != "Y")
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.CompaniesHouseNumberRequiredMessage, BulkUpdateErrors.CompaniesHouseNumberRequired));
        }

        if (!string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.companies_house_number))) && row.GetField(nameof(CompaniesHouseCompany.companies_house_number)).Length > 8)
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.InvalidCompaniesHouseNumberLengthErrorMessage, BulkUpdateErrors.InvalidCompaniesHouseNumberLengthError));
        }

        if (row.GetField(nameof(CompaniesHouseCompany.companies_house_number)).Any(char.IsWhiteSpace))
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.SpacesInCompaniesHouseNumberErrorMessage, BulkUpdateErrors.SpacesInCompaniesHouseNumberError));
        }

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.parent_child))))
        {
            errors.Add(
                CreateError(
                    lineNumber, rawData, BulkUpdateErrors.ParentOrChildRequiredMessage, BulkUpdateErrors.ParentOrChildRequired));
        }

        if (!string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))))
        {
            var franchiseeVal = row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant));
            if (franchiseeVal != "Y")
            {
                errors.Add(
                    CreateError(
                        lineNumber, rawData, BulkUpdateErrors.FranchiseeLicenseeTenantInvalidMessage, BulkUpdateErrors.FranchiseeLicenseeTenantInvalid));
            }
        }

        if (row.ColumnCount > CsvFileValidationConditions.MaxNumberOfColumnsAllowed)
        {
            errors.Add(
                   CreateError(
                       lineNumber, rawData, BulkUpdateErrors.InvalidDataFoundInRowMessage, BulkUpdateErrors.InvalidDataFoundInRow));
        }

        return errors;
    }

    private static UploadFileErrorModel CreateError(int fileLineNumber, string rawDataRow, string message, int errorNumber)
    {
        return new UploadFileErrorModel
        {
            FileLineNumber = fileLineNumber,
            FileContent = rawDataRow,
            Message = message,
            ErrorNumber = errorNumber,
            IsError = true
        };
    }
}