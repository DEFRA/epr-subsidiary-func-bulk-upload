using EPR.SubsidiaryBulkUpload.Application.Extensions;
using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class WebCrawlerService : IWebCrawlerService
{
    public int GetCompaniesHouseFileDownloadCount(string downloadPagePath)
    {
        var web = new HtmlWeb();
        var htmlDocument = web.Load(downloadPagePath);
        var listItems = htmlDocument.DocumentNode.SelectNodes("//ul/li");
        var expectedFileCount = 99;

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

        return expectedFileCount;
    }
}
