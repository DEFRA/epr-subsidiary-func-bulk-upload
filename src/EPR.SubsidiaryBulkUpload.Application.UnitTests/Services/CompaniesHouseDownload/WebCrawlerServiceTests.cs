using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging.Abstractions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class WebCrawlerServiceTests
{
    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadCount_ShouldReturnCorrectFileCout()
    {
        // Arrange
        var downloadPagePath = "https://download/";
        var htmlWeb = new Mock<HtmlWeb>();
        var abc = new HtmlDocument();
        abc.LoadHtml("<html>...</html>");

        htmlWeb.Setup(hw => hw.LoadFromWebAsync(It.IsAny<string>())).ReturnsAsync(abc);

        var webCrawlerService = new WebCrawlerService(NullLogger<FileDownloadService>.Instance, htmlWeb.Object);

        // Act
        var result = await webCrawlerService.GetCompaniesHouseFileDownloadCount(downloadPagePath);

        // Assert
        result.Should().Be(7);
    }
}
