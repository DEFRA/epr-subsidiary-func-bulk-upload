using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using CsvHelper.Configuration.Attributes;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompany
{
    private static readonly string[] MemberNames = { "organisation_name" };

    required public string organisation_id { get; set; }

    [Optional]
    public string subsidiary_id { get; set; }

    [CustomValidation(typeof(CompaniesHouseCompany), nameof(ValidateName))]
    required public string organisation_name { get; set; }

    public string companies_house_number { get; set; }

    required public string parent_child { get; set; }

    [Optional]
    public string franchisee_licensee_tenant { get; set; }

    required public string joiner_date { get; set; }

    required public string reporting_type { get; set; }

    required public string nation_code { get; set; }

    [Optional]
    public OrganisationDto? Organisation { get; init; }

    [Optional]
    public bool AccountExists { get; set; }

    [Optional]
    public DateTimeOffset? AccountCreatedOn { get; set; }

    [Optional]
    public string RawRow { get; set; }

    [Optional]
    public int FileLineNumber { get; set; }

    [Ignore]
    public List<UploadFileErrorModel> Errors { get; set; }

    [Ignore]
    public List<UploadFileErrorModel> ErrorsExcluded { get; set; }

    public HttpStatusCode? StatusCode { get; set; }

    public static ValidationResult ValidateName(string organisation_name, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(organisation_name))
        {
            return new ValidationResult("Invalid organisation_name format.", MemberNames);
        }

        return ValidationResult.Success;
    }
}
