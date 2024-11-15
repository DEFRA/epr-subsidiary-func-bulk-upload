using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
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

            try
            {
                csv.Read();
                csv.ReadHeader();
            }
            catch (Exception ex)
            {
                var errors = BulkUpdateErrors.FileIsInvalidNoHeaderNoDataMessage;
                var fileErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    Errors = new List<Models.UploadFileErrorModel>()
                };

                fileErrors.Errors.Add(new Models.UploadFileErrorModel
                {
                    FileContent = string.Empty,
                    Message = errors,
                    ErrorNumber = BulkUpdateErrors.FileIsInvalidNoHeaderNoData,
                    IsError = true,
                    FileLineNumber = 0
                });

                _logger.LogError(ex, BulkUpdateErrors.FileIsInvalidNoHeaderNoDataMessage);

                rows.Add(fileErrors);
                return rows;
            }

            csv.ValidateHeader<FileUploadHeader>();
            if (csv.InvalidHeaderErrors is { Count: > 0 })
            {
                StringBuilder errMess = new StringBuilder();
                errMess.Append(BulkUpdateErrors.InvalidHeaderOrMissingHeadersMessage);
                errMess.Append(string.Join(",", csv.InvalidHeaderErrors));
                var companyHeaderErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    Errors = new List<Models.UploadFileErrorModel>()
                };

                companyHeaderErrors.Errors.Add(new Models.UploadFileErrorModel
                {
                    FileContent = string.Join(",", csv.HeaderRecord),
                    Message = errMess.ToString(),
                    ErrorNumber = BulkUpdateErrors.InvalidHeader,
                    IsError = true,
                    FileLineNumber = 1
                });

                _logger.LogError("Invalid header count {Count}. Column header(s) missing: #### {Message} #### ", csv.InvalidHeaderErrors.Count, errMess.ToString());
                rows.Add(companyHeaderErrors);
                return rows;
            }

            if (csv.ExtraHeaderErrors is { Count: > 0 })
            {
                StringBuilder errMess = new StringBuilder();
                errMess.Append(BulkUpdateErrors.FileIsInvalidWithExtraHeadersMessage);
                errMess.Append(string.Join(",", csv.ExtraHeaderErrors));
                var companyHeaderErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    Errors = new List<Models.UploadFileErrorModel>()
                };

                companyHeaderErrors.Errors.Add(new Models.UploadFileErrorModel
                {
                    FileContent = string.Join(",", csv.HeaderRecord),
                    Message = errMess.ToString(),
                    ErrorNumber = BulkUpdateErrors.FileIsInvalidWithExtraHeaders,
                    IsError = true,
                    FileLineNumber = 1
                });

                _logger.LogError("Invalid header count {Count}. Column header(s) extra: #### {Message} #### ", csv.ExtraHeaderErrors.Count, errMess.ToString());
                rows.Add(companyHeaderErrors);
                return rows;
            }

            rows = csv.GetRecords<CompaniesHouseCompany>().ToList();

            return rows;
        }
    }
}