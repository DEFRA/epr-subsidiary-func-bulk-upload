using CsvHelper.Configuration.Attributes;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

/// <summary>
/// Test class for defining "Parameter" members required by the CsvReader's ClassMap.
/// </summary>
public class TestCsvReaderParamClass
{
    public TestCsvReaderParamClass([Name("parameter1")] string param1)
    {
        parameter1 = param1;
    }

    public string parameter1 { get; set; }
}