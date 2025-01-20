using System.Net;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
public interface ISubsidiaryService
{
    Task<OrganisationResponseModel?> GetCompanyByCompaniesHouseNumber(string companiesHouseNumber);

    Task<OrganisationModel?> GetCompanyByOrgId(CompaniesHouseCompany company);

    Task<HttpStatusCode> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel);

    Task<HttpStatusCode> AddSubsidiaryRelationshipAsync(SubsidiaryAddModel subsidiaryAddModel);

    Task<bool> GetSubsidiaryRelationshipAsync(int parentOrganisationId, int subsidiaryOrganisationId);

    Task<UserOrganisation> GetSystemUserAndOrganisation();

    Task<OrganisationResponseModel?> GetCompanyByCompanyName(string companyName);

    Task<OrganisationResponseModel?> GetCompanyByReferenceNumber(string referenceNumber);
}