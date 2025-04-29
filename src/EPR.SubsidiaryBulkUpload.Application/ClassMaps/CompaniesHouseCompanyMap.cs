using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.ClassMaps;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompanyMap : ClassMap<CompaniesHouseCompany>
{
    private readonly ISubsidiaryService _organisationService = null;

    public CompaniesHouseCompanyMap(bool includeSubsidiaryJoinerColumns, ISubsidiaryService organisationService, bool enableNationInSub)
    {
        IncludeSubsidiaryJoinerColumns = includeSubsidiaryJoinerColumns;
        EnableNationInSub = enableNationInSub;

        _organisationService = organisationService;
        Map(m => m.organisation_id).Index(0).Validate(field => !field.Equals(null));
        Map(m => m.subsidiary_id);
        Map(m => m.organisation_name).Validate(field => !field.Equals(string.Empty));
        Map(m => m.companies_house_number).Validate(field => !field.Equals(string.Empty));
        Map(m => m.ErrorsExcluded).Convert(args => GetRowValidationErrorsNotRequired(args.Row));
        Map(m => m.parent_child).Validate(field => !field.Equals(string.Empty));
        Map(m => m.franchisee_licensee_tenant);
        Map(m => m.Errors).Index(6).Convert(args => GetRowValidationErrors(args.Row));
        Map(m => m.RawRow).Convert(args => args.Row.Context.Reader.Parser.RawRecord);
        Map(m => m.FileLineNumber).Convert(args => args.Row.Context.Reader.Parser.Row);

        if (IncludeSubsidiaryJoinerColumns)
        {
            Map(m => m.joiner_date);
            Map(m => m.reporting_type);
        }

        if (EnableNationInSub)
        {
            Map(m => m.nation_code);
        }
    }

    private static bool IncludeSubsidiaryJoinerColumns { get; set; }

    private static bool EnableNationInSub { get; set; }

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

        if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.companies_house_number))) && string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.franchisee_licensee_tenant))) && !string.Equals(row.GetField(nameof(CompaniesHouseCompany.parent_child)), "parent", StringComparison.OrdinalIgnoreCase))
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

        if (IncludeSubsidiaryJoinerColumns && EnableNationInSub)
        {
            if (row.ColumnCount > CsvFileValidationConditions.MaxNumberOfColumnsAllowed)
            {
                errors.Add(
                       CreateError(
                           lineNumber, rawData, BulkUpdateErrors.InvalidDataFoundInRowMessage, BulkUpdateErrors.InvalidDataFoundInRow));
            }
        }
        else
        {
            if (IncludeSubsidiaryJoinerColumns)
            {
                if (row.ColumnCount > 8)
                {
                    errors.Add(
                        CreateError(
                            lineNumber, rawData, BulkUpdateErrors.InvalidDataFoundInRowMessage, BulkUpdateErrors.InvalidDataFoundInRow));
                }
            }

            if (EnableNationInSub)
            {
                if (row.ColumnCount > 7)
                {
                    errors.Add(
                        CreateError(
                            lineNumber, rawData, BulkUpdateErrors.InvalidDataFoundInRowMessage, BulkUpdateErrors.InvalidDataFoundInRow));
                }
            }
            else
            {
                if (row.ColumnCount > 6)
                {
                    errors.Add(
                           CreateError(
                               lineNumber, rawData, BulkUpdateErrors.InvalidDataFoundInRowMessage, BulkUpdateErrors.InvalidDataFoundInRow));
                }
            }
        }

        if (IncludeSubsidiaryJoinerColumns)
        {
            if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.joiner_date))) && !string.Equals(row.GetField(nameof(CompaniesHouseCompany.parent_child)), "parent", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    CreateError(
                        lineNumber, rawData, BulkUpdateErrors.JoinerDateRequiredMessage, BulkUpdateErrors.JoinerDateRequired));
            }

            if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.reporting_type))) && !string.Equals(row.GetField(nameof(CompaniesHouseCompany.parent_child)), "parent", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    CreateError(
                        lineNumber, rawData, BulkUpdateErrors.ReportingTypeRequiredMessage, BulkUpdateErrors.ReportingTypeRequired));
            }

            if (!string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.reporting_type))) && !string.Equals(row.GetField(nameof(CompaniesHouseCompany.parent_child)), "parent", StringComparison.OrdinalIgnoreCase))
            {
                var reportingType = row.GetField(nameof(CompaniesHouseCompany.reporting_type));

                switch (reportingType)
                {
                    case "SELF":
                    case "GROUP":
                    case "Self":
                    case "Group":
                    case "self":
                    case "group":
                        break;
                    default:
                        errors.Add(
                            CreateError(
                                lineNumber, rawData, BulkUpdateErrors.ReportingTypeValidValueCheckMessage, BulkUpdateErrors.ReportingTypeValidValueCheck));
                        break;
                }
            }

            string[] formats = { "dd/MM/yyyy", "dd/MMM/yyyy" };
            DateTime dateValue;
            if (!string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.joiner_date))))
            {
                var jointerDate = row.GetField(nameof(CompaniesHouseCompany.joiner_date));
                if (!DateTime.TryParseExact(jointerDate, formats, new CultureInfo("en-GB"), DateTimeStyles.None, out dateValue))
                {
                    errors.Add(
                        CreateError(
                            lineNumber, rawData, BulkUpdateErrors.JoinerDateFormatIncorrectMessage, BulkUpdateErrors.JoinerDateFormatIncorrect));
                }
            }
        }

        if (EnableNationInSub)
        {
            if (string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.nation_code))))
            {
                errors.Add(
                    CreateError(
                        lineNumber, rawData, BulkUpdateErrors.NationCodeRequiredMessage, BulkUpdateErrors.NationCodeRequired));
            }
            else
            {
                if (!Enum.TryParse(typeof(NationCode), row.GetField(nameof(CompaniesHouseCompany.nation_code)), ignoreCase: true, out _))
                {
                    errors.Add(
                        CreateError(
                            lineNumber, rawData, BulkUpdateErrors.NationCodeValidValueCheckMessage, BulkUpdateErrors.NationCodeValidValueCheck));
                }
            }
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

    private List<UploadFileErrorModel> GetRowValidationErrorsNotRequired(IReaderRow row)
    {
        var errors = new List<UploadFileErrorModel>();
        var lineNumber = row.Context.Reader.Parser.Row;
        var rawData = row.Context.Reader.Parser.RawRecord;

        if (IncludeSubsidiaryJoinerColumns && string.IsNullOrEmpty(row.GetField(nameof(CompaniesHouseCompany.joiner_date))) && string.Equals(row.GetField(nameof(CompaniesHouseCompany.parent_child)), "child", StringComparison.OrdinalIgnoreCase))
        {
            var orgReferenceNumber = row.GetField(nameof(CompaniesHouseCompany.organisation_id));
            var companies_house_number = row.GetField(nameof(CompaniesHouseCompany.companies_house_number));
            var responseParent = _organisationService.GetCompanyByReferenceNumber(orgReferenceNumber).GetAwaiter().GetResult();
            var responseChild = _organisationService.GetCompanyByCompaniesHouseNumber(companies_house_number).GetAwaiter().GetResult();
            var orgRelationship = responseChild?.OrganisationRelationship;

            if (orgRelationship != null && orgRelationship.FirstOrganisationId == responseParent.id && orgRelationship.JoinerDate == null)
            {
                errors.Add(
                    CreateError(
                        lineNumber, rawData, BulkUpdateErrors.JoinerDateNotRequiredToValidateForExistingRecordMessage, BulkUpdateErrors.JoinerDateNotRequiredToValidateForExistingRecord));
            }
        }

        return errors;
    }
}