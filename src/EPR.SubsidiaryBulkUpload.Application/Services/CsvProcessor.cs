﻿using System.Globalization;
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

            if (stream.Position != 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            /*
            using var reader1 = new StreamReader(stream);

            string line;
            while ((line = await reader1.ReadLineAsync()) != null)
            {
                _logger.LogInformation("Line = '{Line}", line);
            }

            stream.Seek(0, SeekOrigin.Begin);
            */

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = true });

            await csv.ReadAsync();
            csv.ReadHeader();
            var header = csv.HeaderRecord;
            _logger.LogInformation("Found csv header of length {Length} - {Values}", header.Length, string.Join(',', header));

            while (await csv.ReadAsync())
            {
                rowCount++;
                var field = csv.GetField(0);

                // var rec = csv.GetRecord<X>();
                _logger.LogInformation("Found csv field {RowCount}\t{Value}", rowCount, field);
            }

            return rowCount;
        }
    }
}