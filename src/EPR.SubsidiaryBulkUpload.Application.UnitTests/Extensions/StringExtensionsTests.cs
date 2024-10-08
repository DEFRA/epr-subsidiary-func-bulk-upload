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
}
