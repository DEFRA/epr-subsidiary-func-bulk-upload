using System.Text;
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
            try
            {
                using (var reader = new StreamReader(stream))

                using (var csv = new CustomCsvReader(reader, configuration))
                {
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

                        throw;
                    }
                    catch (ReaderException ex)
                    {
                        _logger.LogError(ex, " Invalid header count {count}", ex.Message);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, " Error occured while processing the CSV file. {count}", ex.Message);
                        throw;
                    }

                    try
                    {
                        csv.ValidateHeader<CompaniesHouseCompanySmart>();
                        List<string> unmappedFields = new();
                        csv.MissingFieldMappingFound = args => unmappedFields.AddRange(args.HeaderNames);
                    }
                    catch (HeaderValidationException ex)
                    {
                        if (ex != null && ex.InvalidHeaders is not null)
                        {
                            var headerJoint = string.Join("\t", ex.InvalidHeaders.Select(x => x.Names[0]));
                            _logger.LogError(ex, "Invalid header. Column header(s) missing: #### {headerJoint} #### ", headerJoint);
                            exceptions.Add(ex);
                        }
                    }
                    catch (UnexpectedHeadersException ex)
                    {
                        if (ex != null && ex.UnexpectedHeaders is not null)
                        {
                            var headerJoint = string.Join("\t", ex.UnexpectedHeaders);
                            _logger.LogError(ex, "Invalid header. Unexpected Header(s): **** {headerJoint} **** ", headerJoint);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, " Error occured while processing the CSV file. {count}", ex.Message);
                        throw;
                    }

                    // csv.ValidateHeader<TD>();
                    if (exceptions.Count > 0)
                    {
                        throw new AggregateException(exceptions);
                    }

                    var logger = new StringBuilder();
                    logger = null;
                    try
                    {
                        if (logger == null)
                        {
                            logger = new StringBuilder();
                        }

                        var map = new CompaniesHouseCompanyMap(logger);
                        csv.Context.RegisterClassMap(map);

                        var recordsList = csv.GetRecords<TD>().ToList();

                        if (logger.Length > 0)
                        {
                            throw new Exception(logger.ToString());
                        }

                        return recordsList;
                    }
                    catch (FieldValidationException ex)
                    {
                        var message = string.Empty;
                        var headerIndex = ex.Context.Reader.CurrentIndex;
                        _logger.LogError(ex, "-Error occured while processing CSV File. {error}", ex.Message);

                        _logger.LogError(ex, "-Total number of Rows in the file {fileRowsCount}", ex.Context.Parser.Count);
                        logger.AppendLine($"-Total number of Rows in the file '{ex.Context.Parser.Count.ToString()}' is not valid!");

                        _logger.LogError(ex, "-Error row number in the file {rownumber}", ex.Context.Parser.Row);
                        logger.AppendLine($"Row Number '{ex.Context.Parser.Row.ToString()}' is not valid!");

                        _logger.LogError(ex, "-Error field in the file {rownumber}", ex.Context.Reader.HeaderRecord[headerIndex].ToLower());
                        logger.AppendLine($"field '{ex.Context.Reader.HeaderRecord[headerIndex].ToLower()}' is not valid!");

                        _logger.LogError(ex, "-Error occured while processing Row : {error}", ex.Context.Parser.RawRecord);
                        logger.AppendLine($"Full row '{ex.Context.Parser.RawRecord}' is not valid!");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, " Fields error occured while processing ProcessStream {error}", ex.Message);
                        throw;
                    }

                    if (logger.Length > 0)
                    {
                        throw new Exception(logger.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error occured while processing ProcessStream {error}", ex.Message);
                throw;
            }

            return new List<TD>();
        }
    }
}
