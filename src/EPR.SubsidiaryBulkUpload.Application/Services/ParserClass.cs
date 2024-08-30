using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class ParserClass(ILogger<ParserClass> logger) : IParserClass
    {
        private readonly ILogger<ParserClass> _logger = logger;

        public (ResponseClass ResponseClass, List<CompaniesHouseCompany> CompaniesHouseCompany) ParseWithHelper(Stream stream, IReaderConfiguration configuration)
        {
            var response = new ResponseClass() { isDone = false, Messages = "None" };
            var rows = new List<CompaniesHouseCompany>();

            try
            {
                rows = ParseFileData(stream, configuration);
                response = new ResponseClass() { isDone = true, Messages = "All Done!" };
            }
            catch (Exception e)
            {
                response = new ResponseClass() { isDone = false, Messages = e.Message };
            }

            return (response, rows);
        }

        private List<CompaniesHouseCompany> ParseFileData(Stream stream, IReaderConfiguration configuration)
        {
            var rows = new List<CompaniesHouseCompany>();
            var exceptions = new List<HeaderValidationException>();

            using (var reader = new StreamReader(stream))
            using (var csv = new CustomCsvReader(reader, configuration))
            {
                try
                {
                    csv.Context.RegisterClassMap<CompaniesHouseCompanyMap>();
                    csv.Read();
                    csv.ReadHeader();
                    csv.ValidateHeader<FileUploadHeader>();
                    rows = csv.GetRecords<CompaniesHouseCompany>().ToList();
                }
                catch (HeaderValidationException ex)
                {
                    if (ex != null && ex.InvalidHeaders is not null)
                    {
                        _logger.LogError(ex, " Invalid header count {Count}", ex.InvalidHeaders);
                        exceptions.Add(ex);
                    }

                    if (ex != null && ex.InvalidHeaders is not null)
                    {
                        var headerJoint = string.Join("\t", ex.InvalidHeaders.Select(x => x.Names[0]));
                        _logger.LogError(ex, "Invalid header. Column header(s) missing: #### {HeaderJoint} #### ", headerJoint);
                        exceptions.Add(ex);
                    }
                }
                catch (UnexpectedHeadersException ex)
                {
                    if (ex != null && ex.UnexpectedHeaders is not null)
                    {
                        var headerJoint = string.Join("\t", ex.UnexpectedHeaders);
                        _logger.LogError(ex, "Invalid header. Unexpected Header(s): **** {HeaderJoint} **** ", headerJoint);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Error occured while processing the CSV file. {Count}", ex.Message);
                    throw;
                }
            }

            return rows;
        }
    }
}