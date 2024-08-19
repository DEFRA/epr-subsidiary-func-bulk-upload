using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, IReaderConfiguration configuration)
        where TM : ClassMap;

    Task<int> ProcessStream(Stream stream);

    Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, IReaderConfiguration configuration)
        where TM : ClassMap;
}
