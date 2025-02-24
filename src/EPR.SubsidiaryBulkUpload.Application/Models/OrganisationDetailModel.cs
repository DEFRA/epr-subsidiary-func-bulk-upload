using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;
[ExcludeFromCodeCoverage]
public class OrganisationDetailModel
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string OrganisationRole { get; set; }

    public string OrganisationType { get; set; }

    public string OrganisationNumber { get; set; }

    public int? NationId { get; set; }

    public string CompaniesHouseNumber { get; set; }
}