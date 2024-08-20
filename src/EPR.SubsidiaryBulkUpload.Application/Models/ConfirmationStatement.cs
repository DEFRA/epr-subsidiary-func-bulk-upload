using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class ConfirmationStatement
{
    public string last_made_up_to { get; set; }

    public string next_due { get; set; }

    public string next_made_up_to { get; set; }

    public bool overdue { get; set; }
}