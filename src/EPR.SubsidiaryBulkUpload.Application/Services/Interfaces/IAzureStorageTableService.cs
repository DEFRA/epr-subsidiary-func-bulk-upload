using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces
{
    public interface IAzureStorageTableService
    {
        public Task<List<Company>> GetAll();
    }
}
