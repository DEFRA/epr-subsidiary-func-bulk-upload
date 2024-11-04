using EPR.SubsidiaryBulkUpload.Application.Comparers;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Comparers
{
    [TestClass]
    public class NullOrEmptyStringEqualityComparerTests()
    {
        [TestMethod]
        [DataRow(null, null, true)]
        [DataRow(null, "", true)]
        [DataRow("", null, true)]
        [DataRow("", "", true)]
        [DataRow(null, "string2", false)]
        [DataRow("string1", null, false)]
        [DataRow("string1", "string1", true)]
        [DataRow("string1", "string2", false)]
        [DataRow("string1", "STRING1", true)]
        public void NullOrEmptyStringEqualityComparer_ShouldReturnExpectedResults(string? string1, string? string2, bool expectedResult)
        {
            var result = NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.Equals(string1, string2);

            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void GetHashCode_ShouldReturnZero_ForNullString()
        {
            var result = NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.GetHashCode((string)null);

            result.Should().Be(0);
        }

        [TestMethod]
        public void GetHashCode_ShouldReturnZero_For_EmptyString()
        {
            var input = string.Empty;
            var result = NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.GetHashCode(string.Empty);

            result.Should().Be(0);
        }

        [TestMethod]
        public void GetHashCode_ShouldReturnZero_For_NonEmptyString()
        {
            var input = string.Empty;
            var result = NullOrEmptyStringEqualityComparer.CaseInsensitiveComparer.GetHashCode("Test");

            result.Should().NotBe(0);
        }
    }
}
