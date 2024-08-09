using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
public interface ISubsidiaryService
{
    Task<OrganisationResponseModel?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber);

    Task<OrganisationModel?> GetCompanyByOrgId(CompaniesHouseCompany company);

    Task<OrganisationModel> GetCompanyByOrgIdFromTableStorage(string companiesHouseNumber);

    Task<string?> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel);

    Task<string?> AddSubsidiaryRelationshipAsync(SubsidiaryAddModel subsidiaryAddModel);

    Task<bool?> GetSubsidiaryRelationshipAysnc(string parentCHNumber, string subsidiaryCHNumber);
}