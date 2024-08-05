using System.Globalization;
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

        public async Task<int> ProcessStream(Stream stream)
        {
            var rowCount = 0;

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true,
            });

            await csv.ReadAsync();
            csv.ReadHeader();
            var header = csv.HeaderRecord;
            _logger.LogInformation("Found csv header of length {Length} - {Values}", header.Length, string.Join(',', header));

            while (await csv.ReadAsync())
            {
                rowCount++;

                // var rec = csv.GetRecord<X>(); // TODO: Add a class map and use it here instead of X
                var field = csv.GetField(0);
                if (rowCount % 1000 == 0)
                {
                    _logger.LogInformation("Found csv field {RowCount}\t{Value}", rowCount, field);
                }
            }

            return rowCount;
        }
    }
}
