using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;
using Microsoft.Extensions.Time.Testing;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class CompaniesHouseDownloadServiceTests
{
    private const string PartialFilename = "BasicCompanyData";
    private const string DownloadPath = "https://download/";
    private Fixture fixture;
    private FakeTimeProvider timeProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
        timeProvider = new();
    }

    [TestMethod]
    public async Task StartDownload_ShouldDownloadAllRequiredFiles()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, DownloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var partialFilePath = $"{DownloadPath}{PartialFilename}-2024-03-01-part";
        var partialFileName = $"{PartialFilename}-2024-03-01-part";
        var numberOfDownloads = 3;

        fixture.Customize<CompaniesHouseFileSetDownloadStatus>(ctx => ctx.With(s => s.CurrentRunExpectedFileCount, numberOfDownloads));

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync(partitionKey)).ReturnsAsync(true);

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip" },
            new() { DownloadFileName = $"{partialFileName}2_{numberOfDownloads}.zip" },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip" }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.Succeeded));

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();
        companiesHouseFilePostService.Setup(chfps => chfps.PostFileAsync(It.IsAny<Stream>(), It.IsAny<string>())).ReturnsAsync(HttpStatusCode.OK);

        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFilePath}1_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFilePath}2_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFilePath}3_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
        downloadStatusStorage.Verify(fds => fds.SetCompaniesHouseFileDownloadStatusAsync(It.IsAny<CompaniesHouseFileSetDownloadStatus>()), Times.Exactly(3));
    }

    [TestMethod]
    public async Task StartDownload_ShouldPostAllDownloadFiles()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, DownloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();
        var partialFileName = $"{PartialFilename}-2024-03-01-part";
        var numberOfDownloads = 3;

        fixture.Customize<CompaniesHouseFileSetDownloadStatus>(ctx => ctx.With(s => s.CurrentRunExpectedFileCount, numberOfDownloads));

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync(partitionKey)).ReturnsAsync(true);

        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip" },
            new() { DownloadFileName = $"{partialFileName}2_{numberOfDownloads}.zip" },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip" }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.Succeeded));

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();
        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}1_{numberOfDownloads}.zip"));
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}2_{numberOfDownloads}.zip"));
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}3_{numberOfDownloads}.zip"));
        downloadStatusStorage.Verify(fds => fds.SetCompaniesHouseFileDownloadStatusAsync(It.IsAny<CompaniesHouseFileSetDownloadStatus>()), Times.Exactly(3));
    }

    [TestMethod]
    public async Task StartDownload_DownloadRemainingFiles()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, DownloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();
        var partialFileName = $"{PartialFilename}-2024-03-01-part";
        var partialFilePath = $"{DownloadPath}{PartialFilename}-2024-03-01-part";
        var numberOfDownloads = 5;

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.Succeeded));

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync(partitionKey)).ReturnsAsync(true);
        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip", DownloadStatus = null },
            new() { DownloadFileName = $"{partialFileName}2_{numberOfDownloads}.zip", DownloadStatus = FileDownloadResponseCode.InvalidFilePathUrl },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip", DownloadStatus = FileDownloadResponseCode.DownloadTimedOut },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip", DownloadStatus = FileDownloadResponseCode.Succeeded },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip", DownloadStatus = FileDownloadResponseCode.Succeeded }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();
        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        fileDownloadService.Verify(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestMethod]
    public async Task StartDownload_SetCorrectCodeWhenDownloadTimeout()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, DownloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();
        var partialFileName = $"{PartialFilename}-2024-03-01-part";
        var partialFilePath = $"{DownloadPath}{PartialFilename}-2024-03-01-part";
        var numberOfDownloads = 3;

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.DownloadTimedOut));

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync(partitionKey)).ReturnsAsync(true);
        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip", DownloadStatus = null },
            new() { DownloadFileName = $"{partialFileName}2_{numberOfDownloads}.zip", DownloadStatus = null },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip", DownloadStatus = null }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();
        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        fileDownloadService.Verify(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        companiesHouseFilePostService.Verify(chfps => chfps.PostFileAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ThrowsException_WhenUnableToConnectToCompaniesHouse()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, DownloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();
        var partialFileName = $"{PartialFilename}-2024-03-01-part";
        var partialFilePath = $"{DownloadPath}{PartialFilename}-2024-03-01-part";
        var numberOfDownloads = 1;

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.FailedToFindFile));

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync(partitionKey)).ReturnsAsync(true);
        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip", DownloadStatus = null }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();
        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        fileDownloadService.Verify(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        companiesHouseFilePostService.Verify(chfps => chfps.PostFileAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task DownloadFiles_ShouldGetNumberOfFilesFromCompaniesHouse()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        var partitionKey = now.ToString("yyyyMM");
        var partialFileName = $"{PartialFilename}-2024-03-01-part";

        var numberOfDownloads = 3;
        var options = fixture.CreateOptions<ApiOptions>();
        var fileDownloadService = new Mock<IFileDownloadService>();

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        var downloadLog = new List<CompaniesHouseFileSetDownloadStatus>
        {
            new() { DownloadFileName = $"{partialFileName}1_{numberOfDownloads}.zip", DownloadStatus = null },
            new() { DownloadFileName = $"{partialFileName}2_{numberOfDownloads}.zip", DownloadStatus = null },
            new() { DownloadFileName = $"{partialFileName}3_{numberOfDownloads}.zip", DownloadStatus = null }
        };
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadListAsync(partitionKey)).ReturnsAsync(downloadLog);

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();

        var webCrawlerService = new Mock<IWebCrawlerService>();
        webCrawlerService.Setup(hw => hw.GetCompaniesHouseFileDownloadCount(It.IsAny<string>())).ReturnsAsync(numberOfDownloads);

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            webCrawlerService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.DownloadFiles(partitionKey);

        // Assert
        downloadStatusStorage.Verify(dlss => dlss.GetCompaniesHouseFileDownloadListAsync(It.IsAny<string>()), Times.Once);
    }
}
