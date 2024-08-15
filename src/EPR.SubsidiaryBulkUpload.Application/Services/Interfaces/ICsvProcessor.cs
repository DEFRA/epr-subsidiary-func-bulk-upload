using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<int> ProcessStream(Stream stream, ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService);

    Task<int> ProcessStream(Stream stream);

    Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, IReaderConfiguration configuration)
        where TM : ClassMap;
}
