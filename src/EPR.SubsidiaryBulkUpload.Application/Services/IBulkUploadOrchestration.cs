using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IBulkUploadOrchestration
{
    public Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel);

    public Task NotifyErrors(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel);
}