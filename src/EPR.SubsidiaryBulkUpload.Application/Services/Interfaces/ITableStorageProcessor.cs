using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ITableStorageProcessor
{
    Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, int filePart = 0, int totalFiles = 0);

    Task<CompanyHouseTableEntity?> GetByCompanyNumber(string companiesHouseNumber, string tableName);
}
