using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class RecordExtraction : IRecordExtraction
{
    public IEnumerable<ParentAndSubsidiaries> ExtractParentsAndSubsidiaries(IEnumerable<CompaniesHouseCompany> source)
    {
        var groups = source.GroupBy(s => s.organisation_id);

        foreach (var group in groups)
        {
            var parent = group.FirstOrDefault(g => string.Equals(g.parent_child, "parent", StringComparison.OrdinalIgnoreCase));

            if (parent == null)
            {
                parent = new CompaniesHouseCompany() { organisation_id = group.Key, organisation_name = "orphan", parent_child = "child", joiner_date = string.Empty, reporting_type = string.Empty, nation_code = string.Empty };
            }

            var subsidiaries = group.Where(g => !string.Equals(g.parent_child, "parent", StringComparison.OrdinalIgnoreCase));

            yield return new ParentAndSubsidiaries { Parent = parent, Subsidiaries = subsidiaries.ToList() };
        }
    }
}
