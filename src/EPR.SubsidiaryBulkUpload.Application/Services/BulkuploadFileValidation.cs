using EPR.SubsidiaryBulkUpload.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

    public class BulkuploadFileValidation
    {
        private readonly ILogger<CompaniesHouseCsvProcessor> _logger;

        public BulkuploadFileValidation(ILogger<CompaniesHouseCsvProcessor> logger)
    {
        _logger = logger;
    }

        public async Task Validate(IEnumerable<CompaniesHouseCompany> data, Guid userId)
        {
            throw new Exception("sdf asdf");
        }
}
