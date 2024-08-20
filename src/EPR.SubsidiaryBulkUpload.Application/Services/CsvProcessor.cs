using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(
        ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;

        public async Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, IReaderConfiguration configuration)
            where TM : ClassMap
        {
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Context.RegisterClassMap<TM>();
                return csv.GetRecords<TD>().ToList();
            }
        }
    }
}
