using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
public interface ISubsidiaryService
{
    Task<Company?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber);

    Task<Company?> GetCompanyByOrgId(CompaniesHouseCompany company);

    Task<Company> GetCompanyByOrgIdFromTableStorage(string companiesHouseNumber);

    Task<string?> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel);

    Task<string?> AddSubsidiaryRelationshipAsync(SubsidiaryAddModel subsidiaryAddModel);
}