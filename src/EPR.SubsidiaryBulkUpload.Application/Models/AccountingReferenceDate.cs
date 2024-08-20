using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class AccountingReferenceDate
{
    public string day { get; set; }

    public string month { get; set; }
}