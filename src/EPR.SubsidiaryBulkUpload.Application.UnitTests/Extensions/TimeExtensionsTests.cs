using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class TimeExtensionsTests
{
    [TestMethod]
    [DataRow(null, null, 0)]
    [DataRow(1, TimeUnit.Milliseconds, 1)]
    [DataRow(1, TimeUnit.Seconds, 1000)]
    [DataRow(1, TimeUnit.Minutes, 60000)]
    public void ToTimeSpan_ForInt_ShouldReturnExpectedResult(int input, TimeUnit units, double expectedResultInMilliseconds)
    {
        // Arrange
        // Act
        var result = input.ToTimespan(units);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedResultInMilliseconds);
    }

    [TestMethod]
    [DataRow(null, null, 0)]
    [DataRow(1.2, TimeUnit.Milliseconds, 1.2)]
    [DataRow(1.5, TimeUnit.Seconds, 1500.0)]
    [DataRow(1.5, TimeUnit.Minutes, 90000.0)]
    public void ToTimeSpan_ForDouble_ShouldReturnExpectedResult(double input, TimeUnit units, double expectedResultInMilliseconds)
    {
        // Arrange
        // Act
        var result = input.ToTimespan(units);

        // Assert
        result.TotalMilliseconds.Should().Be(expectedResultInMilliseconds);
    }

    [TestMethod]
    public void ToTimeSpan_ForUnknownValue_ShouldThrowException()
    {
        // Arrange
        var units = (TimeUnit)999;

        // Act
        Func<TimeSpan> act = () => 100.ToTimespan(units);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}
