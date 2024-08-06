using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class Company
{
    public Company()
    {
    }

    public string Organisation_Id { get; set; }

    public string Subsidiary_Id { get; set; }

    public string Organisation_Name { get; set; }

    public string Companies_House_Number { get; set; }

    public string Parent_child { get; set; }

    public string Franchisee_licensee_tenant { get; set; }

    public DateTimeOffset? AccountCreatedOn { get; set; }
}
