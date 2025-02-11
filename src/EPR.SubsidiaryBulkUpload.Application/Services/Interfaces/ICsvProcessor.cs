using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<IEnumerable<TD>> ProcessStreamWithMapping<TD, TM>(Stream stream, IReaderConfiguration configuration)
        where TM : ClassMap;

    Task<IEnumerable<T>> ProcessStream<T>(Stream stream, IReaderConfiguration configuration);
}