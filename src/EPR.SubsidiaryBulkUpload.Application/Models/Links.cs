using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class Links
{
    public string self { get; set; }

    public string filing_history { get; set; }

    public string officers { get; set; }
}