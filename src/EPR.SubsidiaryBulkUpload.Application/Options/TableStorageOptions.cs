using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class TableStorageOptions
{
    public const string SectionName = "TableStorage";

    public string ConnectionString { get; set; }

    public string CompaniesHouseOfflineDataTableName { get; set; }
}
