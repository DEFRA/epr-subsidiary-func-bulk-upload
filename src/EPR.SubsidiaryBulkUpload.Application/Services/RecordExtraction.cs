using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class RecordExtraction : IRecordExtraction
{
    public IEnumerable<ParentWithChildren> ExtractParentsAndChildren(IEnumerable<CompaniesHouseCompany> source)
    {
        return Enumerable.Empty<ParentWithChildren>();
    }
}
