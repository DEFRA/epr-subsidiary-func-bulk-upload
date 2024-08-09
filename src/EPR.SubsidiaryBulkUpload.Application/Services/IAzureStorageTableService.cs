using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public interface IAzureStorageTableService
    {
        public Task<List<Company>> GetAll();
    }
}
