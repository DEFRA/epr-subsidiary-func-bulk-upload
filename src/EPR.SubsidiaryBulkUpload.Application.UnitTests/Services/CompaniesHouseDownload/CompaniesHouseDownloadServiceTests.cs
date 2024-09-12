using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;
using Microsoft.Extensions.Time.Testing;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class CompaniesHouseDownloadServiceTests
{
    private Fixture fixture;
    private FakeTimeProvider timeProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
        timeProvider = new();
    }

    [TestMethod]
    public async Task ShouldDownloadAllRequiredFiles()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var downloadPath = "https://download/";

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, downloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var partialFileName = $"{downloadPath}{CompaniesHouseDownloadService.PartialFilename}-2024-03-01-part";

        var numberOfDownloads = 3;

        fixture.Customize<CompaniesHouseFileSetDownloadStatus>(ctx => ctx.With(s => s.CurrentRunExpectedFileCount, numberOfDownloads));
        var downloadStatus = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync()).ReturnsAsync(downloadStatus);

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.Succeeded));

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            Mock.Of<ICompaniesHouseFilePostService>(),
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFileName}1_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFileName}2_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
        fileDownloadService.Verify(fds => fds.GetStreamAsync($"{partialFileName}3_{numberOfDownloads}.zip", It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task ShouldPostAllDownloadFiles()
    {
        // Arrange
        var month = 3;
        var year = 2024;
        var now = new DateTimeOffset(year, month, 5, 7, 9, 11, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);

        var downloadPath = "https://download/";

        fixture.Customize<ApiOptions>(ctx => ctx.With(a => a.CompaniesHouseDataDownloadUrl, downloadPath));
        var options = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var partialFileName = $"{CompaniesHouseDownloadService.PartialFilename}-2024-03-01-part";

        var numberOfDownloads = 3;

        fixture.Customize<CompaniesHouseFileSetDownloadStatus>(ctx => ctx.With(s => s.CurrentRunExpectedFileCount, numberOfDownloads));
        var downloadStatus = fixture.Create<CompaniesHouseFileSetDownloadStatus>();

        var downloadStatusStorage = new Mock<IDownloadStatusStorage>();
        downloadStatusStorage.Setup(dss => dss.GetCompaniesHouseFileDownloadStatusAsync()).ReturnsAsync(downloadStatus);

        var fileDownloadService = new Mock<IFileDownloadService>();
        fileDownloadService.Setup(fds => fds.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, FileDownloadResponseCode.Succeeded));

        var companiesHouseFilePostService = new Mock<ICompaniesHouseFilePostService>();

        var downloadService = new CompaniesHouseDownloadService(
            fileDownloadService.Object,
            downloadStatusStorage.Object,
            companiesHouseFilePostService.Object,
            options,
            timeProvider);

        // Act
        await downloadService.StartDownload();

        // Assert
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}1_{numberOfDownloads}.zip"));
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}2_{numberOfDownloads}.zip"));
        companiesHouseFilePostService.Verify(fps => fps.PostFileAsync(stream, $"{partialFileName}3_{numberOfDownloads}.zip"));
    }
}
