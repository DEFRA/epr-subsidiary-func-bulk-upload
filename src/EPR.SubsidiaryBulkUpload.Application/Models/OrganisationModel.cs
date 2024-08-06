using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class OrganisationModel
{
    [Required]
    public string? OrganisationId { get; set; } = null!;

    public string SubsidiaryId { get; set; }

    [Required]
    public string? CompaniesHouseNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrganisationName { get; set; } = null!;

    // [Required]
    // public AddressModel Address { get; set; } = null!;
    public bool ValidatedWithCompaniesHouse { get; set; }
}
