using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;
using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

[ExcludeFromCodeCoverage(Justification = "Provider class to enable unit testing of HtmlAgilityPack")]
public class HtmlWebProvider : IHtmlWebProvider
{
    private readonly HtmlWeb _htmlWeb;

    public HtmlWebProvider()
    {
        _htmlWeb = new HtmlWeb();
    }

    public async Task<HtmlDocument> LoadFromWebAsync(string downloadPath)
    {
        return await _htmlWeb.LoadFromWebAsync(downloadPath);
    }
}
