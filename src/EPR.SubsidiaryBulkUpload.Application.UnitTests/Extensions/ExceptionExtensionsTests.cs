using EPR.SubsidiaryBulkUpload.Application.Extensions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class ExceptionExtensionsTests
{
    [TestMethod]
    public void GetAllMessages_ForException_ShouldReturnExpectedMessage()
    {
        var exception = new Exception("Test exception");
        var result = exception.GetAllMessages();

        result.Should().Be("Test exception");
    }

    [TestMethod]
    public void GetAllMessages_ForNullException_WithEmptyMessage_ShouldReturnEmptyMessage()
    {
        var exception = (Exception)null;
        var result = exception.GetAllMessages();

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void GetAllMessages_ForException_WithEmptyMessage_ShouldReturnEmptyMessage()
    {
        var exception = new Exception(string.Empty);
        var result = exception.GetAllMessages();

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void GetAllMessages_ForInnerException_ShouldReturnExpectedMessage()
    {
        var innerException = new Exception("Test inner exception.");
        var exception = new Exception("Test exception.", innerException);
        var result = exception.GetAllMessages();

        result.Should().Be("Test exception. Test inner exception.");
    }

    [TestMethod]
    public void GetAllMessages_ForNestedInnerException_ShouldReturnExpectedMessage()
    {
        var nestedInnerException = new Exception("Test nested inner exception.");
        var innerException = new Exception("Test inner exception.", nestedInnerException);
        var exception = new Exception("Test exception.", innerException);
        var result = exception.GetAllMessages();

        result.Should().Be("Test exception. Test inner exception. Test nested inner exception.");
    }
}
