using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public class HtmlWebProvider : IHtmlWebProvider
{
    private readonly HtmlWeb _htmlWeb;

    public HtmlWebProvider(ISubsidiaryService subsidiaryService)
    {
        _htmlWeb = new HtmlWeb();
    }

    public async Task<HtmlDocument> LoadFromWebAsync(string downloadPath)
    {
        return await _htmlWeb.LoadFromWebAsync(downloadPath);
    }
}
