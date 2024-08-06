using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class CountryDto
{
    public string? Name { get; set; }

    public string? Iso { get; set; }
}
