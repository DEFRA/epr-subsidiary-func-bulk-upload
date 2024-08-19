namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public class CsvProcessor : ICsvProcessor
{
    public async Task<int> ProcessStream(Stream stream)
    {
        return 0;
    }

    public async Task<IEnumerable<T>> ProcessStreamToObject<T>(Stream stream, T streamObj)
    {
        return null;
    }
}
