using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class NextAccounts
{
    public string due_on { get; set; }

    public bool overdue { get; set; }

    public string period_end_on { get; set; }

    public string period_start_on { get; set; }
}