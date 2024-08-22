using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Configs;

[ExcludeFromCodeCoverage]
public class RedisConfig
{
    public const string SectionName = "RedisConfig";

    public string ConnectionString { get; set; } = null!;
}
