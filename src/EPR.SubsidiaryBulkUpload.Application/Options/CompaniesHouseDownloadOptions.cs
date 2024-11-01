using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class CompaniesHouseDownloadOptions : ApiResilienceOptions
{
    public const string SectionName = "CompaniesHouseDownload";

    public string DataDownloadUrl { get; set; } = null!;

    public string DownloadPage { get; set; } = null!;
}