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
            var response = new ResponseClass { isDone = false, Messages = "None" };
            var rows = new List<CompaniesHouseCompany>();

            try
            {
                rows = ParseFileData(stream, configuration);
                response = new ResponseClass { isDone = true, Messages = "All Done!" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing the CSV file. {Message}", ex.Message);
                response = new ResponseClass { isDone = false, Messages = ex.Message };
            }

            return (response, rows);
        }

        private List<CompaniesHouseCompany> ParseFileData(Stream stream, IReaderConfiguration configuration)
        {
            var rows = new List<CompaniesHouseCompany>();

            using var reader = new StreamReader(stream);
            using var csv = new CustomCsvReader(reader, configuration);

            csv.Context.RegisterClassMap<CompaniesHouseCompanyMap>();
            csv.Read();
            csv.ReadHeader();

            csv.ValidateHeader<FileUploadHeader>();
            if (csv.InvalidHeaderErrors is { Count: > 0 })
            {
                var errors = string.Join("\t", csv.InvalidHeaderErrors);
                var companyHeaderErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    Errors = errors,
                    UploadFileErrorModel = new Models.UploadFileErrorModel
                    {
                        FileContent = "headererror-Invalid",
                        Message = errors
                    }
                };

                _logger.LogError("Invalid header count {Count}. Column header(s) missing: #### {Message} #### ", csv.InvalidHeaderErrors.Count, companyHeaderErrors.UploadFileErrorModel.Message);
                rows.Add(companyHeaderErrors);
                return rows;
            }

            rows = csv.GetRecords<CompaniesHouseCompany>().ToList();

            return rows;
        }
    }
}