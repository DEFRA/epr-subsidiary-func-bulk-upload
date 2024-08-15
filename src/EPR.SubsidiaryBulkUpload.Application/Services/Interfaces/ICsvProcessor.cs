namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ICsvProcessor
{
    Task<int> ProcessStream(Stream stream);

    Task<IEnumerable<T>> ProcessStreamToObject<T>(Stream stream, T streamObj);
}
