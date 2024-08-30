using CsvHelper.Configuration.Attributes;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

public class CompaniesHouseResponseFromCompaniesHouse
{
    [Optional]
    public OrganisationDto? Organisation { get; init; }

    [Optional]
    public bool AccountExists { get; set; }

    [Optional]
    public DateTimeOffset? AccountCreatedOn { get; set; }
}
