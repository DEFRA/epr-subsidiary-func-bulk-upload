using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class CompaniesHouseResponse
{
    public OrganisationDto? Organisation { get; set; }

    public bool AccountExists => AccountCreatedOn is not null;

    public DateTimeOffset? AccountCreatedOn { get; set; }
}
