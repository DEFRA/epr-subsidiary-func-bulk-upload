using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class RecordExtraction : IRecordExtraction
{
    public IEnumerable<ParentAndSubsidiaries> ExtractParentsAndChildren(IEnumerable<CompaniesHouseCompany> source)
    {
        var groups = source.GroupBy(s => s.organisation_id);

        foreach (var group in groups)
        {
            var parent = group.SingleOrDefault(g => g.parent_child == "Parent");

            var children = group.Where(g => g.parent_child != "Parent");

            if (parent != null && children.Any())
            {
                yield return new ParentAndSubsidiaries { Parent = parent, Children = children.ToList() };
            }
        }
    }
}
