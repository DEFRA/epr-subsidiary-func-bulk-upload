using System.ComponentModel.DataAnnotations;
using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class EnumExtensionsTests
{
    private const string HasNameName = "HasName";

    private enum TestEnumeration
    {
        [Display(Name = HasNameName)]
        HasName,
        NoName,
    }

    [TestMethod]
    public void GetDisplayName_ReturnsDisplayName_WhenDisplayNameAnnotationExists()
    {
        // Arrange / Act
        var result = TestEnumeration.HasName.GetDisplayName();

        // Assert
        result.Should().Be(HasNameName);
    }

    [TestMethod]
    public void GetDisplayName_ReturnsNull_WhenDisplayNameAnnotationDoesNotExist()
    {
        // Arrange / Act
        var result = TestEnumeration.NoName.GetDisplayName();

        // Assert
        result.Should().BeNull();
    }
}