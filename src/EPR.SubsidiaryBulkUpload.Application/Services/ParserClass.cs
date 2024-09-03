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
            var headersValidations = new List<InvalidHeader>();
            var validationErrors = new List<string>();
            using (var reader = new StreamReader(stream))
            using (var csv = new CustomCsvReader(reader, configuration))
            {
                try
                {
                    csv.Context.RegisterClassMap<CompaniesHouseCompanyMap>();
                    csv.Read();
                    csv.ReadHeader();

                    try
                    {
                        csv.ValidateHeader<FileUploadHeader>();
                        if (csv.InvalidHeaderErrors.Count > 0)
                        {
                            _logger.LogError("Invalid header count {Count}", csv.InvalidHeaderErrors);
                            var headerJoint = string.Join("\t", csv.InvalidHeaderErrors);
                            var companyHeaderErrors = new CompaniesHouseCompany()
                            {
                                companies_house_number = string.Empty,
                                organisation_name = string.Empty,
                                organisation_id = string.Empty,
                                parent_child = string.Empty,
                                UploadFileErrorModel = new Models.UploadFileErrorModel()
                                {
                                    FileContent = "headererror-Invalid",
                                    Message = headerJoint
                                }
                            };

                            _logger.LogError("Invalid header. Column header(s) missing: #### {HeaderJoint} #### ", headerJoint);
                            rows.Add(companyHeaderErrors);
                            return rows;
                        }

                    }
                    catch (Exception e)
                    {
                        if (e is HeaderValidationException)
                        {
                            var errorType = (HeaderValidationException)e;
                            if (errorType.InvalidHeaders is not null)
                            {
                                _logger.LogError(e, " Invalid header count {Count}", errorType.InvalidHeaders);
                                var headerJoint = string.Join("\t", errorType.InvalidHeaders.Select(x => x.Names[0]));
                                var companyHeaderErrors = new CompaniesHouseCompany()
                                {
                                    companies_house_number = string.Empty,
                                    organisation_name = string.Empty,
                                    organisation_id = string.Empty,
                                    parent_child = string.Empty,
                                    UploadFileErrorModel = new Models.UploadFileErrorModel()
                                    {
                                        FileContent = "headererror-Invalid",
                                        Message = headerJoint
                                    }
                                };

                                _logger.LogError(e, "Invalid header. Column header(s) missing: #### {HeaderJoint} #### ", headerJoint);
                                rows.Add(companyHeaderErrors);
                                return rows;
                            }
                        }
                        else if (e is UnexpectedHeadersException)
                        {
                            var errorType = (UnexpectedHeadersException)e;
                            if (errorType.UnexpectedHeaders is not null)
                            {
                                _logger.LogError(e, "Invalid header. Unexpected Header(s): **** {UnexpectedHeaders} **** ", errorType.UnexpectedHeaders);

                                var headerJoint = string.Join("\t", errorType.UnexpectedHeaders);
                                var companyHeaderErrors = new CompaniesHouseCompany()
                                {
                                    companies_house_number = string.Empty,
                                    organisation_name = string.Empty,
                                    organisation_id = string.Empty,
                                    parent_child = string.Empty,
                                    UploadFileErrorModel = new Models.UploadFileErrorModel()
                                    {
                                        FileContent = "headererror-Unexpected",
                                        Message = headerJoint
                                    }
                                };

                                rows.Add(companyHeaderErrors);
                                return rows;
                            }
                        }
                    }

                    rows = csv.GetRecords<CompaniesHouseCompany>().ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured while processing the CSV file. {Message}", ex.Message);
                }
            }

            return rows;
        }
    }
}