using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;

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
    public void ToUserRequestModel_ReturnsNull_WhenMetadataMissingUserId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenMetadataMissingOrganisationId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenInvalidUserId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "InvalidGUID" },
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenInvalidOrganisationId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" },
                { "OrganisationId", "InvalidGUID" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsUserRequestModel_WhenValidGuids()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" },
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Guid.Parse("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b"), result.UserId);
        Assert.AreEqual(Guid.Parse("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c"), result.OrganisationId);
    }

    [TestMethod]
    public void GenerateKey_ReturnsNull_WhenUserRequestModelIsNull()
    {
        // Arrange
        UserRequestModel userRequestModel = null;
        string suffix = "TestSuffix";

        // Act
        var result = userRequestModel.GenerateKey(suffix);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GenerateKey_ReturnsConcatenatedString_WhenValidUserRequestModelAndSuffix()
    {
        // Arrange
        var userRequestModel = new UserRequestModel
        {
            UserId = Guid.Parse("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b"),
            OrganisationId = Guid.Parse("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c")
        };
        string suffix = "TestSuffix";

        // Act
        var result = userRequestModel.GenerateKey(suffix);

        // Assert
        var expected = "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4bf2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4cTestSuffix";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GenerateKey_ReturnsConcatenatedStringWithoutSuffix_WhenEmptySuffix()
    {
        // Arrange
        var userRequestModel = new UserRequestModel
        {
            UserId = Guid.Parse("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b"),
            OrganisationId = Guid.Parse("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c")
        };
        string suffix = string.Empty;

        // Act
        var result = userRequestModel.GenerateKey(suffix);

        // Assert
        var expected = "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4bf2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GenerateKey_ReturnsConcatenatedStringWithNullAsSuffix_WhenNullSuffix()
    {
        // Arrange
        var userRequestModel = new UserRequestModel
        {
            UserId = Guid.Parse("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b"),
            OrganisationId = Guid.Parse("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c")
        };
        string suffix = null;

        // Act
        var result = userRequestModel.GenerateKey(suffix);

        // Assert
        var expected = "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4bf2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c";
        Assert.AreEqual(expected, result);
    }
}
