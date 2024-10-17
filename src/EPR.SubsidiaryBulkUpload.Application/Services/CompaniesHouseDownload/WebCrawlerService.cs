using EPR.SubsidiaryBulkUpload.Application.Extensions;
using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class WebCrawlerService : IWebCrawlerService
{
    public int GetCompaniesHouseFileDownloadCount(string path)
    {
        var web = new HtmlWeb();
        var htmlDocument = web.Load(path);
        var listItems = htmlDocument.DocumentNode.SelectNodes("//ul/li");
        var expectedFileCount = 7;

        for (int i = 0; i < listItems.Count; i++)
        {
            HtmlNode? item = listItems[i];
            if (!item.InnerText.Contains("part"))
            {
                var filePartC = item.InnerText.ToFilePartNumberAndCount();
                expectedFileCount = filePartC.TotalFiles;
                break;
            }

            var filePart = item.InnerText.ToFilePartNumberAndCount();
        }

        return expectedFileCount;
    }
}
