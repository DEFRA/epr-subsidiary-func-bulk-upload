using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Configs;
[ExcludeFromCodeCoverage]
public class ApiConfig
{
    public const string SectionName = "ApiConfig";

    public string CompaniesHouseLookupBaseUrl { get; set; } = null!;

    public string AccountServiceClientId { get; set; } = null!;

    public string Certificate { get; set; } = null!;

    public int Timeout { get; set; }

    public string SubsidiaryServiceBaseUrl { get; set; } = null!;

    public string StorageConnectionString { get; set; } = null!;
}
