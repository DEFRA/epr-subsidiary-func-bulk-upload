using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IBulkUploadOrchestration
{
    public Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, Guid userId);
}