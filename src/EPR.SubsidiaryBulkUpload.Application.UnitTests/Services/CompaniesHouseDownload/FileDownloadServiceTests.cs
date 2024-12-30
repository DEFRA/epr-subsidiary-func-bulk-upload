using System.Net;
using System.Text;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class FileDownloadServiceTests
{
    private Fixture _fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
    }

    [TestMethod]
    public async Task ShouldDownloadTheFile()
    {
        // Arrange
        var filePath = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<string>();

        var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
            });

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath);

        // Assert
        actual.Stream.Should().NotBeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.Succeeded);
    }

    [TestMethod]
    public async Task ShouldReplyNothingWhenFilePathNotValid()
    {
        // Arrange
        var filePath = "Invalid URI";
        var content = _fixture.Create<string>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws<HttpRequestException>();

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath);

        // Assert
        actual.Stream.Should().BeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.InvalidFilePathUrl);
    }

    [TestMethod]
    public async Task ShouldReplyNothingWhenFileMPathNotFound()
    {
        // Arrange
        var filePath = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<string>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws<HttpRequestException>();

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath);

        // Assert
        actual.Stream.Should().BeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.FailedToFindFile);
    }

    [TestMethod]
    public async Task ShouldReplyNothingWhenTimeoutOccurs()
    {
        // Arrange
        var filePath = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<string>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws<TaskCanceledException>();

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath);

        // Assert
        actual.Stream.Should().BeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.DownloadTimedOut);
    }

    [TestMethod]
    public async Task ShouldReplyNothingWhenDownloadCancelled()
    {
        // Arrange
        var filePath = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<string>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws<OperationCanceledException>();

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath);

        // Assert
        actual.Stream.Should().BeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.DownloadCancelled);
    }

    [TestMethod]
    public async Task ShouldReplyNothingWhenDownloadCancelledLocally()
    {
        // Arrange
        var filePath = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<string>();

        var handler = new Mock<HttpMessageHandler>();

        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var client = new HttpClient(handler.Object);

        var fileDownloadService = new FileDownloadService(client, NullLogger<FileDownloadService>.Instance);

        // Act
        var actual = await fileDownloadService.GetStreamAsync(filePath, cancellationTokenSource.Token);

        // Assert
        actual.Stream.Should().BeNull();
        actual.ResponseCode.Should().Be(FileDownloadResponseCode.DownloadCancelled);
    }
}
