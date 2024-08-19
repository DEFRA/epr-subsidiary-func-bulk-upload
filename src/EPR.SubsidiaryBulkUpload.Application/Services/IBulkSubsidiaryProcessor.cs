using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface IBulkSubsidiaryProcessor
{
    Task Process(IEnumerable<CompaniesHouseCompany> children, CompaniesHouseCompany parent, OrganisationResponseModel parentorg, Guid userId);
}
