using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class LastAccounts
{
    public string made_up_to { get; set; }

    public string period_end_on { get; set; }

    public string period_start_on { get; set; }

    public string type { get; set; }
}