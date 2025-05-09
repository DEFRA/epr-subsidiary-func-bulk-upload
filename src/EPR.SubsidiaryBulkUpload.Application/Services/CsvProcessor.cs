﻿using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(IParserClass parserClass, ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;
        private readonly IParserClass _parserClass = parserClass;

        public async Task<IEnumerable<T>> ProcessStream<T>(Stream stream, IReaderConfiguration configuration)
        {
            using var streamReader = new StreamReader(stream);
            using var csv = new CsvReader(streamReader, configuration);

            var records = csv.GetRecords<T>().ToList();

            _logger.LogInformation("Found {RowCount} csv rows", records.Count);

            return records;
        }

        public async Task<IEnumerable<TD>> ProcessStreamWithMapping<TD, TM>(Stream stream, IReaderConfiguration configuration)
             where TM : ClassMap
        {
            try
            {
                var (_, theList) = _parserClass.ParseWithHelper(stream, configuration);
                return (IEnumerable<TD>)theList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing ProcessStream {Error}", ex.Message);
                throw;
            }

            return Enumerable.Empty<TD>();
        }
    }
}
