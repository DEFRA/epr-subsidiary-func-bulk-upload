using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface IRecordExtraction
{
    IEnumerable<ParentWithChildren> ExtractParentsAndChildren(IEnumerable<CompaniesHouseCompany> source);

}

public class ParentWithChildren
{
    public CompaniesHouseCompany Parent { get; set; }

    public List<CompaniesHouseCompany> Children { get; set; }
}
