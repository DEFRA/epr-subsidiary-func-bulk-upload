using System.Diagnostics.CodeAnalysis;
using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.ClassMaps;
using EPR.SubsidiaryBulkUpload.Application.Constants;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    public class ParserClass(ILogger<ParserClass> logger, IFeatureManager featureManager, ISubsidiaryService organisationService) : IParserClass
    {
        private readonly ILogger<ParserClass> _logger = logger;
        private readonly ISubsidiaryService _organisationService = organisationService;

        public (ResponseClass ResponseClass, List<CompaniesHouseCompany> CompaniesHouseCompany) ParseWithHelper(Stream stream, IReaderConfiguration configuration)
        {
            var response = new ResponseClass { isDone = false, Messages = "None" };
            var rows = new List<CompaniesHouseCompany>();
            var enableSubsidiaryJoinerColumns = featureManager.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerColumns).GetAwaiter().GetResult();
            var enableSubsidiaryNationColumn = featureManager.IsEnabledAsync(FeatureFlags.EnableSubsidiaryNationColumn).GetAwaiter().GetResult();

            try
            {
                rows = ParseFileData(stream, configuration, enableSubsidiaryJoinerColumns, enableSubsidiaryNationColumn);
                response = new ResponseClass { isDone = true, Messages = "All Done!" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing the CSV file. {Message}", ex.Message);
                response = new ResponseClass { isDone = false, Messages = ex.Message };
            }

            return (response, rows);
        }

        [ExcludeFromCodeCoverage]
        private static void ProcessCompanyRowErrors(List<CompaniesHouseCompany> rows)
        {
            foreach (var companyRow in rows)
            {
                if (companyRow!.ErrorsExcluded!.Count > 0 && companyRow!.Errors.Count > 0)
                {
                    var errorsNotRequired = companyRow.Errors.Where(e => e.ErrorNumber == BulkUpdateErrors.JoinerDateRequired).ToList();
                    if (errorsNotRequired.Count > 0)
                    {
                        foreach (var errorToRemove in errorsNotRequired)
                        {
                            companyRow.Errors.Remove(errorToRemove);
                        }
                    }
                }
            }
        }

        private List<CompaniesHouseCompany> ParseFileData(Stream stream, IReaderConfiguration configuration, bool includeSubsidiaryJoinerColumns, bool enableSubsidiaryNationColumn)
        {
            var rows = new List<CompaniesHouseCompany>();
            using var reader = new StreamReader(stream);
            using var csv = new CustomCsvReader(reader, configuration);
            csv.Context.RegisterClassMap(new CompaniesHouseCompanyMap(includeSubsidiaryJoinerColumns, _organisationService, enableSubsidiaryNationColumn));

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
                    joiner_date = string.Empty,
                    reporting_type = string.Empty,
                    nation_code = string.Empty,
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

            if (includeSubsidiaryJoinerColumns && enableSubsidiaryNationColumn)
            {
                csv.ValidateHeader<FileUploadHeaderWithSubsidiaryJoinerAndNationCodeColumns>();
            }
            else if (includeSubsidiaryJoinerColumns)
            {
                csv.ValidateHeader<FileUploadHeaderWithSubsidiaryJoinerColumns>();
            }
            else if (enableSubsidiaryNationColumn)
            {
                csv.ValidateHeader<FileUploadHeaderWithNationCodeColumn>();
            }
            else
            {
                csv.ValidateHeader<FileUploadHeader>();
            }

            if (csv.InvalidHeaderErrors is { Count: > 0 })
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append(BulkUpdateErrors.InvalidHeaderOrMissingHeadersMessage);
                errorMessage.Append(string.Join(",", csv.InvalidHeaderErrors));
                var companyHeaderErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    joiner_date = string.Empty,
                    reporting_type = string.Empty,
                    nation_code = string.Empty,
                    Errors = new List<Models.UploadFileErrorModel>()
                };

                companyHeaderErrors.Errors.Add(new Models.UploadFileErrorModel
                {
                    FileContent = string.Join(",", csv.HeaderRecord),
                    Message = errorMessage.ToString(),
                    ErrorNumber = BulkUpdateErrors.InvalidHeader,
                    IsError = true,
                    FileLineNumber = 1
                });

                _logger.LogError("Invalid header count {Count}. Column header(s) missing: #### {Message} #### ", csv.InvalidHeaderErrors.Count, errorMessage.ToString());
                rows.Add(companyHeaderErrors);
                return rows;
            }

            if (csv.ExtraHeaderErrors is { Count: > 0 })
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append(BulkUpdateErrors.FileIsInvalidWithExtraHeadersMessage);
                errorMessage.Append(string.Join(",", csv.ExtraHeaderErrors));
                var companyHeaderErrors = new CompaniesHouseCompany
                {
                    companies_house_number = string.Empty,
                    organisation_name = string.Empty,
                    organisation_id = string.Empty,
                    parent_child = string.Empty,
                    joiner_date = string.Empty,
                    reporting_type = string.Empty,
                    nation_code = string.Empty,
                    Errors = new List<Models.UploadFileErrorModel>()
                };

                companyHeaderErrors.Errors.Add(new Models.UploadFileErrorModel
                {
                    FileContent = string.Join(",", csv.HeaderRecord),
                    Message = errorMessage.ToString(),
                    ErrorNumber = BulkUpdateErrors.FileIsInvalidWithExtraHeaders,
                    IsError = true,
                    FileLineNumber = 1
                });

                _logger.LogError("Invalid header count {Count}. Column header(s) extra: #### {Message} #### ", csv.ExtraHeaderErrors.Count, errorMessage.ToString());
                rows.Add(companyHeaderErrors);
                return rows;
            }

            rows = csv.GetRecords<CompaniesHouseCompany>().ToList();

            ProcessCompanyRowErrors(rows);

            return rows;
        }
    }
}