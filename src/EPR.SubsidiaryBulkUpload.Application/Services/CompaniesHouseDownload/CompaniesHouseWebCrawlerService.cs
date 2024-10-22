using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class CompaniesHouseWebCrawlerService(ILogger<CompaniesHouseWebCrawlerService> logger, IHtmlWebProvider htmlWebProvider)
    : ICompaniesHouseWebCrawlerService
{
    private readonly ILogger<CompaniesHouseWebCrawlerService> _logger = logger;
    private readonly IHtmlWebProvider _htmlWeb = htmlWebProvider;

    public async Task<int> GetCompaniesHouseFileDownloadCount(string downloadPagePath)
    {
        var expectedFileCount = 0;

        try
        {
            var htmlDocument = await _htmlWeb.LoadFromWebAsync(downloadPagePath);
            var listItems = htmlDocument.DocumentNode.SelectNodes("//ul/li");

            if (listItems == null)
            {
                _logger.LogError("No files to download from CompaniesHouse{DownloadPagePath}", downloadPagePath);
                return expectedFileCount;
            }

            for (var i = 0; i < listItems.Count; i++)
            {
                var item = listItems[i];
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
            _logger.LogError(ex, "Failed to access download page at {DownloadPagePath}", downloadPagePath);
            expectedFileCount = 0;
            throw new FileDownloadException("Failed to access download page");
        }

        return expectedFileCount;
    }
}
