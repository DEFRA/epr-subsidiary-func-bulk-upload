using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class CompaniesHouseWebCrawlerServiceTests
{
    private const string HtmlValidPage = """
            <html lang="en">
            <head>
            </head>
            <body>
            <div>
                <h2>How to download Company data</h2>
                <h2>Company data as multiple files:</h2>
                <ul>
                    <li><a href="BasicCompanyData-2024-03-01-part1_7.zip">BasicCompanyData-2024-03-01-part1_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part2_7.zip">BasicCompanyData-2024-03-01-part2_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part3_7.zip">BasicCompanyData-2024-03-01-part3_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part4_7.zip">BasicCompanyData-2024-03-01-part4_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part5_7.zip">BasicCompanyData-2024-03-01-part5_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part6_7.zip">BasicCompanyData-2024-03-01-part6_7.zip</a></li>
                    <li><a href="BasicCompanyData-2024-03-01-part7_7.zip">BasicCompanyData-2024-03-01-part7_7.zip</a></li>
                </ul>
                <strong>Last Updated:</strong> 01/03/2024
            </div>
            </body></html>
            """;

    private const string HtmlNoAvailableDownloadsPage = """
            <html lang="en">
            <head>
            </head>
            <body>
            <div>
                Empty
            </div>
            </body></html>
            """;

    private Mock<ILogger<CompaniesHouseWebCrawlerService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<CompaniesHouseWebCrawlerService>>();
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadCount_FileCountSuccessful()
    {
        // Arrange
        var htmlWebMock = new Mock<IHtmlWebProvider>();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(HtmlValidPage);
        var downloadPath = "https://download.success/success.html";

        htmlWebMock
            .Setup(x => x.LoadFromWebAsync(downloadPath))
            .ReturnsAsync(htmlDocument);

        var companiesHouseWebCrawlerService = new CompaniesHouseWebCrawlerService(_loggerMock.Object, htmlWebMock.Object);

        // Act
        var result = await companiesHouseWebCrawlerService.GetCompaniesHouseFileDownloadCount(downloadPath);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadCount_NoFilesAvailableForDownload()
    {
        // Arrange
        var htmlWebMock = new Mock<IHtmlWebProvider>();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(HtmlNoAvailableDownloadsPage);
        var downloadPath = "https://download.success/success.html";

        htmlWebMock
            .Setup(x => x.LoadFromWebAsync(downloadPath))
            .ReturnsAsync(htmlDocument);

        var companiesHouseWebCrawlerService = new CompaniesHouseWebCrawlerService(_loggerMock.Object, htmlWebMock.Object);

        // Act
        var result = await companiesHouseWebCrawlerService.GetCompaniesHouseFileDownloadCount(downloadPath);

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task GetCompaniesHouseFileDownloadCount_ThrowsException()
    {
        // Arrange
        var htmlWebMock = new Mock<IHtmlWebProvider>();
        var htmlDocument = new HtmlDocument();
        var brokenDownloadPath = "https://download.broken/broken.html";

        htmlWebMock.Setup(tc => tc.LoadFromWebAsync(brokenDownloadPath))
            .ThrowsAsync(new Exception("error"));

        var companiesHouseWebCrawlerService = new CompaniesHouseWebCrawlerService(_loggerMock.Object, htmlWebMock.Object);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<FileDownloadException>(async () =>
            await companiesHouseWebCrawlerService.GetCompaniesHouseFileDownloadCount(brokenDownloadPath));
    }
}
