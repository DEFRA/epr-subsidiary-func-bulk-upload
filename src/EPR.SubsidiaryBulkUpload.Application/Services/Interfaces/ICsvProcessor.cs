using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, CsvConfiguration configuration)
        where TM : ClassMap;

    Task<bool?> Validate(IEnumerable<CompaniesHouseCompany> data, Guid userId);
}