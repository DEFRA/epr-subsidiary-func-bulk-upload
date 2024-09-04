using System.Text.Json;
using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class JsonExtensionsTests
{
    private readonly JsonDocument _jsonDocument = JsonDocument.Parse(
        """
        {
            "anElement": {
               "myInt32": 123,
               "myNull": null,
               "myTrueBool": true,
               "myFalseBool": false,
               "myString": "my value"
            }
       }
       """);

    [TestMethod]
    [DataRow("myString", "my value")]
    [DataRow("missingString", null)]
    [DataRow("myNull", null)]
    public void JsonElement_SafeGetString_Data_Tests(string propertyName, string expectedResult)
    {
        var prop = _jsonDocument.RootElement.GetProperty("anElement");
        var result = prop.GetStringFromJsonElement(propertyName);

        result.Should().Be(expectedResult);
    }
}
