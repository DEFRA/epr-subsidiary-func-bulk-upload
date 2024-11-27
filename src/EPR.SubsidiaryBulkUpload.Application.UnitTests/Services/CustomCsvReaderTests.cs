using System.Text;
using CsvHelper;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CustomCsvReaderTests
{
    private Fixture _fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
    }

    [TestMethod]
    public void ValidateHeader_Should_Not_Add_Errors_When_Headers_Are_Valid()
    {
        // Arrange
        var header = "p1\n";
        var line1 = "vp1\n";

        string[] all = [header, line1];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());
        var configuration = CsvConfigurations.BulkUploadCsvConfiguration;
        using var reader = new StreamReader(stream);
        var map = new TestCsvReaderClassParamMap();
        var invalidHeaders = new List<InvalidHeader>(); // Initialize with appropriate values
        var customCsvReader = new CustomCsvReaderWrapper(reader, configuration);

        customCsvReader.Context.RegisterClassMap<TestCsvReaderClassParamMap>();
        customCsvReader.Read();
        customCsvReader.ReadHeader();

        // Act
        customCsvReader.PublicValidateHeader(map, invalidHeaders);

        // Assert
        invalidHeaders.Should().BeEmpty();
    }
}