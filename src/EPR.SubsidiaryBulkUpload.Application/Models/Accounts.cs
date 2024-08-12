using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class Accounts
{
    public AccountingReferenceDate accounting_reference_date { get; set; }

    public LastAccounts last_accounts { get; set; }

    public NextAccounts next_accounts { get; set; }

    public string next_due { get; set; }

    public string next_made_up_to { get; set; }

    public bool overdue { get; set; }
}