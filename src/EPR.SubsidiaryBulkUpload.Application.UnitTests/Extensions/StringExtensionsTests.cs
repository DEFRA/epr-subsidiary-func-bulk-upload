using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("CompanyDataFile20240801.csv", "")]
    [DataRow("CompanyDataFile-2024-08-01.csv", "2024-08-01")]
    [DataRow("BasicCompanyData-2024-10-01-part1_3.zip", "2024-10-01")]
    public void ToPartitionKey_ShouldFormatCorrectly(string input, string expectedResult)
    {
        var result = input.ToPartitionKey();

        result.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow(null, 0, 0)]
    [DataRow("", 0, 0)]
    [DataRow("CompanyDataFile-2024-08-01.csv", 0, 0)]
    [DataRow("BasicCompanyData-2024-12-01-part18.zip", 0, 0)]
    [DataRow("BasicCompanyData-2024-12-01-part1_8.zip", 1, 8)]
    [DataRow("BasicCompanyData-2024-12-01-part5_10.zip", 5, 10)]
    [DataRow("BasicCompanyData-2024-12-01-part150_211.zip", 150, 211)]
    public void ToFilePartNumberAndCount_ShouldFormatCorrectly(string input, int expectedFilePart, int expectedFileCount)
    {
        var result = input.ToFilePartNumberAndCount();

        result.Should().NotBeNull();
        result.PartNumber.Should().Be(expectedFilePart);
        result.TotalFiles.Should().Be(expectedFileCount);
    }
}
