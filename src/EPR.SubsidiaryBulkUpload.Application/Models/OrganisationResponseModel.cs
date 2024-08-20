using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class OrganisationResponseModel
{
    public DateTime createdOn { get; set; }

    public int id { get; set; }

    public Guid? ExternalId { get; set; }

    public string organisationType { get; set; }

    public object producerType { get; set; }

    public string companiesHouseNumber { get; set; }

    public string name { get; set; }

    public Address address { get; set; }

    public bool validatedWithCompaniesHouse { get; set; }

    public bool isComplianceScheme { get; set; }

    public string referenceNumber { get; set; }

    public string nation { get; set; }
}
