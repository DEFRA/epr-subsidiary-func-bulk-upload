using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;

public class TestCsvReaderClassParamMap : ClassMap<TestCsvReaderParamClass>
{
    public TestCsvReaderClassParamMap()
    {
        Parameter("param1").Name("p1");
    }
}