using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IBulkUploadOrchestration
{
    Task NotifyStart(UserRequestModel userRequestModel);

    Task Orchestrate(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel);

    Task NotifyErrors(IEnumerable<CompaniesHouseCompany> data, UserRequestModel userRequestModel);
}