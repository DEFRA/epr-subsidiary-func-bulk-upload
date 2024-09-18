using System.Text;
using CsvHelper;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules dont yet support collection expressions")]
public class CustomCsvReaderTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
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
        var customCsvReader = new CustomCsvReaderTest(reader, configuration);

        customCsvReader.Context.RegisterClassMap<TestCsvReaderClassParamMap>();
        customCsvReader.Read();
        customCsvReader.ReadHeader();

        // Act
        customCsvReader.PublicValidateHeader(map, invalidHeaders);

        // Assert
        invalidHeaders.Should().BeEmpty();
    }
}