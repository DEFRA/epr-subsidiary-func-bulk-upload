using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models.Events;

[ExcludeFromCodeCoverage]
public abstract class AbstractEvent
{
    public abstract EventType Type { get; }
}