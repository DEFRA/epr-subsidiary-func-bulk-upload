using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;
[ExcludeFromCodeCoverage]
public class AddSubsidiariesFigures
{
    public AddSubsidiariesFigures()
    {
        SubsidiaryWithExistingRelationships = new List<CompaniesHouseCompany>();
        SubsidiariesWithNoExistingRelationships = new List<CompaniesHouseCompany>();
        NewAddedSubsidiaries = new List<CompaniesHouseCompany>();
        NotAddedSubsidiaries = new List<CompaniesHouseCompany>();
        CompaniesHouseAPIErrorListReported = new List<CompaniesHouseCompany>();
        DuplicateSubsidiaries = new List<CompaniesHouseCompany>();
        AlreadyExistCompanies = new List<CompaniesHouseCompany>();
    }

    public List<CompaniesHouseCompany> SubsidiaryWithExistingRelationships { get; set; }

    public List<CompaniesHouseCompany> SubsidiariesWithNoExistingRelationships { get; set; }

    public int NewAddedSubsidiariesRelationships { get; set; }

    public int UpdatedSubsidiariesRelationships { get; set; }

    public List<CompaniesHouseCompany> NewAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> UpdatedAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> NotAddedSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> CompaniesHouseAPIErrorListReported { get; set; }

    public List<CompaniesHouseCompany> DuplicateSubsidiaries { get; set; }

    public List<CompaniesHouseCompany> AlreadyExistCompanies { get; set; }
}