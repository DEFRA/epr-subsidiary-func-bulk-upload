namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICompaniesHouseCsvProcessor
{
    Task<int> ProcessStream(Stream stream);

    Task<IEnumerable<T>> ProcessStreamToObject<T>(Stream stream, T streamObj);
}
