using EPR.SubsidiaryBulkUpload.Application.Extensions;
using FluentAssertions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("CompanyDataFile20240801.csv", "")]
    [DataRow("CompanyDataFile-2024-08-01.csv", "2024-08-01")]
    public void ToFindPartitionKey_ShouldFormatCorrectly(string input, string expectedResult)
    {
        var result = input.ToPartitionKeyFormat();

        result.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("CompanyDataFile.csv", "")]
    [DataRow("CompanyDataFile-2024-07-01.csv", "2024-07-01")]
    public void ToPartitionKeyFormat_ShouldFormatCorrectly(string input, string expectedResult)
    {
        var result = input.ToPartitionKeyFormat();

        result.Should().Be(expectedResult);
    }
}
