using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ParentAndSubsidiaries
{
    public CompaniesHouseCompany Parent { get; set; }

    public List<CompaniesHouseCompany> Subsidiaries { get; set; }
}