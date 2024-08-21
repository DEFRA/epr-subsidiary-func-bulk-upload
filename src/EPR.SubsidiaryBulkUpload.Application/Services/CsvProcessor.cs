using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(
        ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;

        public async Task<IEnumerable<TD>> ProcessStreamWithMapping<TD, TM>(Stream stream, IReaderConfiguration configuration)
            where TM : ClassMap
        {
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Context.RegisterClassMap<TM>();
                return csv.GetRecords<TD>().ToList();
            }
        }

        public async Task<IEnumerable<T>> ProcessStream<T>(Stream stream, IReaderConfiguration configuration)
        {
            using var streamReader = new StreamReader(stream);
            using var csv = new CsvReader(streamReader, configuration);

            var records = csv.GetRecords<T>().ToList();

            _logger.LogInformation("Found {RowCount} csv rows", records.Count);

            return records;
        }
    }
}
