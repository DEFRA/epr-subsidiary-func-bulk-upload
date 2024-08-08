namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<int> ProcessStream(Stream stream, ISubsidiaryService organisationService, ICompaniesHouseLookupService companiesHouseLookupService);

    Task<int> ProcessStream(Stream stream);
}
