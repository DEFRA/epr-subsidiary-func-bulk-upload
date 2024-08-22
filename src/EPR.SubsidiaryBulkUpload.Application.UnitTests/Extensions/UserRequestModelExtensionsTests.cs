using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class UserRequestModelExtensionsTests
{
    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenMetadataIsNull()
    {
        // Arrange
        IDictionary<string, string> metadata = null;

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenUserIdIsMissing()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "OrganisationId", "org123" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenOrganisationIdIsMissing()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "user123" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsUserRequestModel_WhenMetadataContainsRequiredKeys()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "user123" },
                { "OrganisationId", "org123" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("user123", result.UserId);
        Assert.AreEqual("org123", result.OrganisationId);
    }

    [TestMethod]
    public void ToUserRequestModel_IgnoresCase_WhenMetadataKeysAreCaseInsensitive()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "userid", "user123" },
                { "organisationid", "org123" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("user123", result.UserId);
        Assert.AreEqual("org123", result.OrganisationId);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsUserRequestModel_WithMixedCaseKeys()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UsErId", "user123" },
                { "OrGaniSationId", "org123" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("user123", result.UserId);
        Assert.AreEqual("org123", result.OrganisationId);
    }
}
