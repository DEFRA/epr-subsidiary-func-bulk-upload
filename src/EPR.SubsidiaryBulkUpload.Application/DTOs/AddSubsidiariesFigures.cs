using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;
[ExcludeFromCodeCoverage]
public class AddSubsidiariesFigures
{
    public List<CompaniesHouseCompany> SubsidiaryWithExistingRelationships { get; set; }

    public List<CompaniesHouseCompany> SubsidiriesWithNoExistingRelationships { get; set; }

    public int NewAddedSubsidiariesRelationships { get; set; }

    public List<CompaniesHouseCompany> NewAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> NotAddedSubsidiaries { get; set; }
}