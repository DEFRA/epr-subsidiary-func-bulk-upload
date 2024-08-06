using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;
[ExcludeFromCodeCoverage]
public class OrganisationDataDto
{
    public DateTime DateOfCreation { get; set; }

    public string? Status { get; set; }

    public string? Type { get; set; }
}
