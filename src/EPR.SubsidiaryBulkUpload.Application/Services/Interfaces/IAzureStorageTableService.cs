using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces
{
    public interface IAzureStorageTableService
    {
        public Task<List<Company>> GetAll();

        public Task<OrganisationModel?> GetByCompanyNumber(string companiesHouseNumber);

        public Task<Company> GetById(string id);
    }
}
