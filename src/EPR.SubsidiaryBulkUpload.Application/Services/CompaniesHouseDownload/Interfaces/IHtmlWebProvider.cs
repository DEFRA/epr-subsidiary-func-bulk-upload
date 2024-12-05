using HtmlAgilityPack;

namespace EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload.Interfaces;

public interface IHtmlWebProvider
{
    Task<HtmlDocument> LoadFromWebAsync(string downloadPath);
}