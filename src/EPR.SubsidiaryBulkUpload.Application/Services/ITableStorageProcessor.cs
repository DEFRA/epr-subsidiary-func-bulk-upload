﻿using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface ITableStorageProcessor
{
    Task WriteToAzureTableStorage(IEnumerable<CompanyHouseTableEntity> records, string tableName, string partitionKey, string connectionString, int batchSize);

    Task<CompanyHouseTableEntity?> GetByCompanyNumber(string companiesHouseNumber, string tableName);
}