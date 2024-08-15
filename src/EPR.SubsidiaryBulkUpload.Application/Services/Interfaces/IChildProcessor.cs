using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface IChildProcessor
{
    Task Process(IEnumerable<CompaniesHouseCompany> children, CompaniesHouseCompany parent, OrganisationResponseModel parentorg);
}
