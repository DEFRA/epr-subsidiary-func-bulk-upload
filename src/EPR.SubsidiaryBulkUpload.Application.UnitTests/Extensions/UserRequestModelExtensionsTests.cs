using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class UserRequestModelExtensionsTests
{
    [TestMethod]
    [DataRow(null, null, null, null, null, null)] // Scenario 1: metadata is null
    [DataRow("user123", null, null, null, null, null)] // Scenario 2: OrganisationId is missing
    [DataRow(null, "org123", null, null, null, null)] // Scenario 3: UserId is missing
    [DataRow("Userid", "Organisationid", "user123", "org123", "user123", "org123")] // Scenario 4: Both keys are present
    [DataRow("USERID", "ORGANISATIONID", "user123", "org123", "user123", "org123")] // Scenario 5: Case-insensitivity check
    [DataRow("UsErId", "OrGaniSationId", "user123", "org123", "user123", "org123")] // Scenario 6: Mixed casing in keys
    public void ToUserRequestModel_TestCases(string userIdKey, string organisationIdKey, string userIdValue, string organisationIdValue, string expectedUserId, string expectedOrganisationId)
    {
        // Arrange
        IDictionary<string, string> metadata = null;

        if (userIdKey != null || organisationIdKey != null)
        {
            metadata = new Dictionary<string, string>();

            if (userIdKey != null)
            {
                metadata[userIdKey] = userIdValue;
            }

            if (organisationIdKey != null)
            {
                metadata[organisationIdKey] = organisationIdValue;
            }
        }

        // Act
        var result = metadata.ToUserRequestModel();

        // Assert
        if (expectedUserId == null && expectedOrganisationId == null)
        {
            Assert.IsNull(result);
        }
        else
        {
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUserId, result.UserId);
            Assert.AreEqual(expectedOrganisationId, result.OrganisationId);
        }
    }
}
