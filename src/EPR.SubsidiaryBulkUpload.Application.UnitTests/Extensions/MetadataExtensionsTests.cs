using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class MetadataExtensionsTests
{
    [TestMethod]
    public void GetFileName_ReturnsNull_WhenMetadataIsNull()
    {
        // Arrange
        IDictionary<string, string> metadata = null;

        // Act
        var result = metadata.GetFileName();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetFileName_ReturnsNull_WhenMetadataMissingFileName()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" }
            };

        // Act
        var result = metadata.GetFileName();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetFileName_ReturnsFileName_WhenValidFileName()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "fileName", "test.csv" },
            };

        // Act
        var result = metadata.GetFileName();

        // Assert
        result.Should().Be("test.csv");
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenMetadataIsNull()
    {
        // Arrange
        IDictionary<string, string> metadata = null;

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        result.Should().BeNull();
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
        result.Should().BeNull();
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
        result.Should().BeNull();
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
        result.Should().BeNull();
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
        result.Should().BeNull();
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsNull_WhenInvalidComplianceId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" },
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" },
                { "ComplianceSchemeId", "InvalidGUID" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsUserRequestModel_WhenValidGuids()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" },
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" },
                { "ComplianceSchemeId", "033593e1-98d3-4451-84a5-465482ed4b53" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b");
        result.OrganisationId.Should().Be("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c");
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
        result.Should().BeNull();
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
        result.Should().Be(expected);
    }

    [TestMethod]
    public void GenerateKey_ReturnsConcatenatedString_WhenValidUserRequestModelWithComplianceSchemeAndSuffix()
    {
        // Arrange
        var userRequestModel = new UserRequestModel
        {
            UserId = Guid.Parse("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b"),
            OrganisationId = Guid.Parse("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c"),
            ComplianceSchemeId = Guid.Parse("033593e1-98d3-4451-84a5-465482ed4b53")
        };
        string suffix = "TestSuffix";

        // Act
        var result = userRequestModel.GenerateKey(suffix);

        // Assert
        var expected = "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4bf2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4cTestSuffix";
        result.Should().Be(expected);
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
        result.Should().Be(expected);
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
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ToUserRequestModel_ReturnsUserRequestModel_WhenValidGuids_WithComplianceSchemeId()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
            {
                { "UserId", "d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b" },
                { "OrganisationId", "f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c" },
                { "ComplianceSchemeId", "033593e1-98d3-4451-84a5-465482ed4b53" }
            };

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("d2c12e8a-0d47-4cd9-b8e1-1f766a5c6e4b");
        result.OrganisationId.Should().Be("f2c12e8a-1d47-4cd9-b8e1-2f766a5c6e4c");
        result.ComplianceSchemeId.Should().Be("033593e1-98d3-4451-84a5-465482ed4b53");
    }
}
