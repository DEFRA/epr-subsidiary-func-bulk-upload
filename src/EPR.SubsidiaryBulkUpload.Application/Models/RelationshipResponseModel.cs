using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;
[ExcludeFromCodeCoverage]
public class RelationshipResponseModel
{
    public string OrganisationNumber { get; set; }

    public string OrganisationName { get; set; }

    public string RelationshipType { get; set; }

    public string CompaniesHouseNumber { get; set; }
}