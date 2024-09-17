using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration.Attributes;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class CompaniesHouseResponse
{
    [Optional]
    public OrganisationDto? Organisation { get; init; }

    [Optional]
    public bool AccountExists { get; set; }

    [Optional]
    public DateTimeOffset? AccountCreatedOn { get; set; }
}
