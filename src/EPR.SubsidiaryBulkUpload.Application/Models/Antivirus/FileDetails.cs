namespace EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileDetails
{
#pragma warning disable CA1822, S2325 // Mark members as static - cannot be static as it breaks serialization
    public string Service => "epr";
#pragma warning restore CA1822, S2325 // Mark members as static

    public Guid Key { get; set; }

    public string Collection { get; set; }

    public string Extension { get; set; }

    public string FileName { get; set; }

    public Guid UserId { get; set; }

    public string UserEmail { get; set; }
}