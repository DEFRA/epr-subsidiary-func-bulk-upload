using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;

/// <summary>
/// Helper class to expose the protected ValidateHeader method for testing.
/// </summary>
public class CustomCsvReaderWrapper : CustomCsvReader
{
    public CustomCsvReaderWrapper(TextReader reader, IReaderConfiguration configuration)
    : base(reader, configuration)
    {
    }

    public void PublicValidateHeader(ClassMap map, List<InvalidHeader> invalidHeaders)
    {
        ValidateHeader(map, invalidHeaders);
    }
}