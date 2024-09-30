using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class OptionsExtensionsTests
{
    [TestMethod]
    [DataRow(null, null, 0)]
    [DataRow(1, TimeUnit.Milliseconds, 1)]
    [DataRow(1, TimeUnit.Seconds, 1000)]
    [DataRow(1, TimeUnit.Minutes, 60000)]
    public void GetTimeSpan_ShouldReturnExpectedResult_ForInt(int input, TimeUnit units, double expectedResultInMilliseconds)
    {
        // Arrange
        var options = new ApiOptions
        {
            CompaniesHouseLookupBaseUrl = "localhost",
            TimeUnits = units,
        };

        // Act
        var result = options.ConvertToTimespan(input);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedResultInMilliseconds);
    }

    [TestMethod]
    [DataRow(null, null, 0)]
    [DataRow(1.2, TimeUnit.Milliseconds, 1.2)]
    [DataRow(1.5, TimeUnit.Seconds, 1500.0)]
    [DataRow(1.5, TimeUnit.Minutes, 90000.0)]
    public void GetTimeSpan_ShouldReturnExpectedResult_ForDouble(double input, TimeUnit units, double expectedResultInMilliseconds)
    {
        // Arrange
        var options = new ApiOptions
        {
            CompaniesHouseLookupBaseUrl = "localhost",
            TimeUnits = units,
        };

        // Act
        var result = options.ConvertToTimespan(input);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedResultInMilliseconds);
    }

    [TestMethod]
    public void GetTimeSpan_ShouldThrowException_ForUnknownValue()
    {
        // Arrange
        var options = new ApiOptions
        {
            CompaniesHouseLookupBaseUrl = "localhost",
            TimeUnits = (TimeUnit)999,
        };

        // Act
        Func<TimeSpan> act = () => options.ConvertToTimespan(100);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}
