using EPR.SubsidiaryBulkUpload.Application.Extensions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class WebCrawlerService(ILogger<FileDownloadService> logger, HtmlWeb htmlWeb)
    : IWebCrawlerService
{
    private const int DefaultFileCount = 7;
    private readonly ILogger<FileDownloadService> _logger = logger;
    private readonly HtmlWeb _htmlWeb = htmlWeb;

    public async Task<int> GetCompaniesHouseFileDownloadCount(string downloadPagePath)
    {
        var expectedFileCount = DefaultFileCount;

        try
        {
            var htmlDocument = await _htmlWeb.LoadFromWebAsync(downloadPagePath);
            var listItems = htmlDocument.DocumentNode.SelectNodes("//ul/li");

            for (int i = 0; i < listItems.Count; i++)
            {
                HtmlNode? item = listItems[i];
                if (item.InnerText.Contains("part"))
                {
                    var filePart = item.InnerText.ToFilePartNumberAndCount();
                    expectedFileCount = filePart.TotalFiles;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to access downlod page at {DownloadPagePath}", downloadPagePath);
            expectedFileCount = 0;
        }

        if (expectedFileCount == 0)
        {
            _logger.LogWarning("No files to download from CompaniesHouse{DownloadPagePath}", downloadPagePath);
        }

        return expectedFileCount;
    }
}
