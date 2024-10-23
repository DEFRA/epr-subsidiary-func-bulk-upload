using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;

public interface IHtmlWebProvider
{
    Task<HtmlDocument> LoadFromWebAsync(string downloadPath);
}