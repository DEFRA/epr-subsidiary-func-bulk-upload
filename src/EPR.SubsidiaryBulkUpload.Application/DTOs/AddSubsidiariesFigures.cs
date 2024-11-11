using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;
[ExcludeFromCodeCoverage]
public class AddSubsidiariesFigures
{
    public AddSubsidiariesFigures()
    {
        NewAddedSubsidiaries = new List<CompaniesHouseCompany>();
        NotAddedSubsidiaries = new List<CompaniesHouseCompany>();
    }

    public List<CompaniesHouseCompany> SubsidiaryWithExistingRelationships { get; set; }

    public List<CompaniesHouseCompany> SubsidiariesWithNoExistingRelationships { get; set; }

    public int NewAddedSubsidiariesRelationships { get; set; }

    public List<CompaniesHouseCompany> NewAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> NotAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> CompaniesHouseAPIErrorListReported { get; set; }
}