using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class CompaniesHouseWebCrawlerServiceTests
{
    private Mock<ILogger<CompaniesHouseWebCrawlerService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<CompaniesHouseWebCrawlerService>>();
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadCount_ThrowsException()
    {
        // Arrange
        var htmlWeb = new HtmlWeb();
        var logger = new Mock<ILogger>();
        var brokenDownloadPath = "https://download.broken/broken.html";

        var companiesHouseWebCrawlerService = new CompaniesHouseWebCrawlerService(_loggerMock.Object, htmlWeb);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
            await companiesHouseWebCrawlerService.GetCompaniesHouseFileDownloadCount(brokenDownloadPath));
    }
}
