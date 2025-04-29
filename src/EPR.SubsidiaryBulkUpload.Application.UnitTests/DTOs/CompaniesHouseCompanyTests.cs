using System.ComponentModel.DataAnnotations;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.DTOs;

[TestClass]
public class CompaniesHouseCompanyTests
{
    private readonly CompaniesHouseCompany _sut = new()
    {
        organisation_id = "1234",
        companies_house_number = "01234567",
        organisation_name = "Test organisation",
        parent_child = "Child",
        joiner_date = "01/10/2024",
        reporting_type = "SELF",
        nation_code = NationCode.EN
    };

    [TestMethod]
    public void ValidateName_ValidatesCorrectly_WhenInputIsValid()
    {
        // Arrange
        var validationContext = new ValidationContext(_sut);

        // Act
        var validationResult = CompaniesHouseCompany.ValidateName("test", validationContext);

        // Assert
        validationResult.Should().Be(ValidationResult.Success);
    }

    [TestMethod]
    public void ValidateName_ValidatesCorrectly_WhenInputIsNull()
    {
        // Arrange
        var validationContext = new ValidationContext(_sut);

        // Act
        var validationResult = CompaniesHouseCompany.ValidateName(null, validationContext);

        // Assert
        validationResult.ErrorMessage.Should().Be("Invalid organisation_name format.");
        validationResult.MemberNames.Should().Contain("organisation_name");
    }

    [TestMethod]
    public void ValidateName_ValidatesCorrectly_WhenInputIsEmptyString()
    {
        // Arrange
        var validationContext = new ValidationContext(_sut);

        // Act
        var validationResult = CompaniesHouseCompany.ValidateName(string.Empty, validationContext);

        // Assert
        validationResult.ErrorMessage.Should().Be("Invalid organisation_name format.");
        validationResult.MemberNames.Should().Contain("organisation_name");
    }
}
