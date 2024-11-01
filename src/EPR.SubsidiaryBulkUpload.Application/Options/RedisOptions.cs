using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = null!;

    public int? TimeToLiveInMinutes { get; set; } = null;
}
