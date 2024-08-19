namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

public class ParentAndSubsidiaries
{
    public CompaniesHouseCompany Parent { get; set; }

    public List<CompaniesHouseCompany> Children { get; set; }
}