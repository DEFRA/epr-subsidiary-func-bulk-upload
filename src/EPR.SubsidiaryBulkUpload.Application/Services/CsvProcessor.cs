using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(
        ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;

        public async Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, CsvConfiguration configuration)
            where TM : ClassMap
        {
            using (var reader = new StreamReader(stream))

            using (var csv = new CustomCsvReader(reader, configuration))
            {
                List<string> unmappedFields = new();
                csv.MissingFieldMappingFound = args => unmappedFields.AddRange(args.HeaderNames);

                var exceptions = new List<HeaderValidationException>();

                // var parser = new CsvParser(configuration);

                /*    csv.Configuration.HeaderValidated = (isValid, headerNames, headerNameIndex, context) =>
                {
                    if (!isValid)
                    {
                        exceptions.Add(new HeaderValidationException(context, headerNames, headerNameIndex, "Your message here."));
                    }
                };*/
                try
                {
                    csv.Read();
                    csv.ReadHeader();
                }
                catch (HeaderValidationException ex)
                {
                    if (ex != null && ex.InvalidHeaders is not null)
                    {
                        _logger.LogError(ex, " Invalid header count {count}", ex.InvalidHeaders);
                        exceptions.Add(ex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Invalid header count {count}", ex.Message);
                }

                try
                {
                    csv.ValidateHeader<CompaniesHouseCompany>();
                }
                catch (HeaderValidationException ex)
                {
                    if (ex != null && ex.InvalidHeaders is not null)
                    {
                        _logger.LogError(ex, " Invalid header count {count}", ex.InvalidHeaders);
                        exceptions.Add(ex);
                    }
                }

                // csv.ValidateHeader<TD>();
                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }

                csv.Context.RegisterClassMap<TM>();
                return csv.GetRecords<TD>().ToList();
            }
        }
    }
}
