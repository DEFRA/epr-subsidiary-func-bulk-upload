using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

/// <summary>
/// Helper class to expose the protected ValidateHeader method for testing.
/// </summary>
public class CustomCsvReaderTest : CustomCsvReader
{
    public CustomCsvReaderTest(TextReader reader, IReaderConfiguration configuration)
    : base(reader, configuration)
    {
    }

    public void PublicValidateHeader(ClassMap map, List<InvalidHeader> invalidHeaders)
    {
        ValidateHeader(map, invalidHeaders);
    }
}