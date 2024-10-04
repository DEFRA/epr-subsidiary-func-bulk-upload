using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class HttpStatusCodeExtensionsTests
{
    [TestMethod]
    public void ShouldDetermineIfSuccessCode()
    {
        // Arrange
        var successStatusCodes = Enum.GetValues(typeof(HttpStatusCode))
                             .Cast<HttpStatusCode>()
                             .Where(code => (int)code >= 200 && (int)code < 300)
                             .ToList();

        // Act
        var results = successStatusCodes.Select(s => (StatusCode: s, Response: HttpStatusCodeExtensions.IsSuccessStatusCode(s)));

        // Assert
        results.Should().AllSatisfy(result => result.Response.Should().BeTrue());
    }

    [TestMethod]
    public void ShouldDetermineIfNotSuccessCode()
    {
        // Arrange
        var successStatusCodes = Enum.GetValues(typeof(HttpStatusCode))
                             .Cast<HttpStatusCode>()
                             .Where(code => (int)code < 200 || (int)code >= 300)
                             .ToList();

        // Act
        var results = successStatusCodes.Select(s => (StatusCode: s, Response: HttpStatusCodeExtensions.IsSuccessStatusCode(s)));

        // Assert
        results.Should().AllSatisfy(result => result.Response.Should().BeFalse());
    }

    [TestMethod]
    public async Task ShouldRunNextTaskWhenFirstProducesSuccessCode()
    {
        // Arrange
        Task<HttpStatusCode> first = Task.FromResult(HttpStatusCode.OK);
        Task<HttpStatusCode> second = Task.FromResult(HttpStatusCode.OK);

        // Act
        var result = await first.ThenIfIsSuccessStatusCode(() => second);

        // Assert
        result.Should().BeSuccessful();
    }

    [TestMethod]
    public async Task ShouldGetNextTaskResponseWhenFirstProducesSuccessCode()
    {
        // Arrange
        var expectedResponse = HttpStatusCode.NotImplemented;
        Task<HttpStatusCode> first = Task.FromResult(HttpStatusCode.OK);
        Task<HttpStatusCode> second = Task.FromResult(expectedResponse);

        // Act
        var result = await first.ThenIfIsSuccessStatusCode(() => second);

        // Assert
        result.Should().Be(expectedResponse);
    }
}
