using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(ILogger<CsvProcessor> logger) : ICsvProcessor
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

                    var slogger = new StringBuilder();
                    try
                    {
                        if (slogger == null)
                        {
                            slogger = new StringBuilder();
                        }

                        csv.Context.RegisterClassMap(new CompaniesHouseCompanyMap());
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

                    try
                    {
                        var recordsList = csv.GetRecords<TD>().ToList();

                        if (slogger.Length > 0)
                        {
                            throw new Exception(slogger.ToString());
                        }

                        return recordsList;
                    }
                    catch (FieldValidationException ex)
                    {
                        var message = string.Empty;
                        var headerIndex = ex.Context.Reader.CurrentIndex;
                        _logger.LogError(ex, "-Error occured while processing CSV File. {error}", ex.Message);

/*                        _logger.LogError(ex, "-Total number of Rows in the file {fileRowsCount}", ex.Context.Parser.Count);
                        slogger.Add($"-Total number of Rows in the file '{ex.Context.Parser.Count.ToString()}' is not valid!");

                        _logger.LogError(ex, "-Error row number in the file {rownumber}", ex.Context.Parser.Row);
                        slogger.Add($"Row Number '{ex.Context.Parser.Row.ToString()}' is not valid!");

                        _logger.LogError(ex, "-Error field in the file {rownumber}", ex.Context.Reader.HeaderRecord[headerIndex].ToLower());
                        slogger.Add($"field '{ex.Context.Reader.HeaderRecord[headerIndex].ToLower()}' is not valid!");

                        _logger.LogError(ex, "-Error occured while processing Row : {error}", ex.Context.Parser.RawRecord);
                        slogger.Add($"Full row '{ex.Context.Parser.RawRecord}' is not valid!");*/
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, " Fields error occured while processing ProcessStream {error}", ex.Message);
                        throw;
                    }

                    if (slogger.Length > 0)
                    {
                        throw new Exception(slogger.ToString());
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

        public async Task<bool?> Validate(IEnumerable<CompaniesHouseCompany> data, Guid userId)
        {
            var result = new List<CsvErrorModel>();
            var slogger = new StringBuilder();

            foreach (var record in data)
            {
                var companyData = (CompaniesHouseCompany)record;

                if (string.IsNullOrEmpty(record.organisation_id))
                {
                    result.Add(new CsvErrorModel
                    {
                        FieldName = "organisation_id",
                        ErrorMessage = "Organisation Id cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(record.companies_house_number))
                {
                    result.Add(new CsvErrorModel
                    {
                        FieldName = "companies_house_number",
                        ErrorMessage = "Organisation Id cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(record.subsidiary_id))
                {
                    result.Add(new CsvErrorModel
                    {
                        FieldName = "subsidiary_id",
                        ErrorMessage = "Organisation Id cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(record.organisation_name))
                {
                    result.Add(new CsvErrorModel
                    {
                        FieldName = "organisation_name",
                        ErrorMessage = "Organisation Id cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(record.parent_child))
                {
                    result.Add(new CsvErrorModel
                    {
                        FieldName = "parent_child",
                        ErrorMessage = "Organisation Id cannot be empty"
                    });
                }

                if (result.Count > 0)
                {
                    _logger.LogError("File Validation Errors in {rownumber} rows ", result.Count);
                    slogger.AppendLine($"File Validation Errors in {result.Count} rows!");

                    // TO DO - send to database
                    throw new Exception(slogger.ToString());
                }
            }

            return true;
        }
    }
}
