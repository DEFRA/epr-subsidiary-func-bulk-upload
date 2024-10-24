using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class OrganisationModel
{
    public OrganisationType OrganisationType { get; set; }

    public ProducerType? ProducerType { get; set; }

    public string? CompaniesHouseNumber { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public AddressModel Address { get; set; } = null!;

    public bool ValidatedWithCompaniesHouse { get; set; }

    public bool IsComplianceScheme { get; set; }

    public string? ReferenceNumber { get; set; }

    public Nation? Nation { get; set; }

    public string? SubsidiaryOrganisationId { get; set; }

    public string? CompaniesHouseCompanyName { get; set; }

    public string? LocalStorageName { get; set; }

    public UploadFileErrorModel Error { get; set; }

    public string? RawContent { get; set; }

    public int FileLineNumber { get; set; }

    public string Franchisee_Licensee_Tenant { get; set; }
}
