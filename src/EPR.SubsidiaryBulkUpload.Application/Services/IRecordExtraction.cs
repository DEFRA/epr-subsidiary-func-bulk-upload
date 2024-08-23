using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IRecordExtraction
{
    IEnumerable<ParentAndSubsidiaries> ExtractParentsAndSubsidiaries(IEnumerable<CompaniesHouseCompany> source);
}