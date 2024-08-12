using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class RegisteredOfficeAddress
{
    public string address_line_1 { get; set; }

    public string country { get; set; }

    public string locality { get; set; }

    public string postal_code { get; set; }
}