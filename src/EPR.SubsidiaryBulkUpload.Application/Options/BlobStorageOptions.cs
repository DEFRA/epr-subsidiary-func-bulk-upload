using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string CompaniesHouseContainerName { get; set; }
}