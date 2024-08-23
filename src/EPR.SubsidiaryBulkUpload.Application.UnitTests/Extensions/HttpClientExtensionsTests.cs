using System.Net.Mime;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using Microsoft.Net.Http.Headers;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Extensions;

[TestClass]
public class HttpClientExtensionsTests
{
    [TestMethod]
    public void ShouldAddJsonHeaderRequest()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        httpClient.AddHeaderAcceptJson();

        // Assert
        httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == HeaderNames.Accept && h.Value.Contains(MediaTypeNames.Application.Json));
    }
}
