using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class CsvProcessor(IParserClass parserClass, ILogger<CsvProcessor> logger) : ICsvProcessor
    {
        private readonly ILogger<CsvProcessor> _logger = logger;
        private readonly IParserClass _parserClass = parserClass;

        public async Task<IEnumerable<TD>> ProcessStream<TD, TM>(Stream stream, CsvConfiguration configuration)
            where TM : ClassMap
        {
            try
            {
                var (response, theList) = _parserClass.ParseWithHelper(stream, configuration);
                return (IEnumerable<TD>)theList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while processing ProcessStream {error}", ex.Message);
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
