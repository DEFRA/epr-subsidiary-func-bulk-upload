using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class RecordExtraction : IRecordExtraction
{
    public IEnumerable<ParentAndSubsidiaries> ExtractParentsAndSubsidiaries(IEnumerable<CompaniesHouseCompany> source)
    {
        var groups = source.GroupBy(s => s.organisation_id);

        foreach (var group in groups)
        {
            var parent = group.SingleOrDefault(g => g.parent_child == "Parent");

            if (parent == null)
            {
                parent = new CompaniesHouseCompany() { organisation_id = group.Key, organisation_name = "orphan", parent_child = "child" };
            }

            var subsidiaries = group.Where(g => g.parent_child != "Parent");

            if (parent != null && subsidiaries.Any())
            {
                yield return new ParentAndSubsidiaries { Parent = parent, Subsidiaries = subsidiaries.ToList() };
            }
        }
    }
}
