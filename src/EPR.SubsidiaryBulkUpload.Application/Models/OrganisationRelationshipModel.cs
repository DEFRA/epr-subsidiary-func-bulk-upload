using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class OrganisationRelationshipModel
{
    public int FirstOrganisationId { get; set; }

    public int SecondOrganisationId { get; set; }

    public int OrganisationRelationshipTypeId { get; set; }

    public int? OrganisationRegistrationTypeId { get; set; }

    public DateTime? RelationFromDate { get; set; }

    public DateTime? CreatedOn { get; set; }

    public int LastUpdatedById { get; set; }

    public DateTime? LastUpdatedOn { get; set; }

    public int LastUpdatedByOrganisationId { get; set; }

    public DateTime? JoinerDate { get; set; }

    public int? ReportingTypeId { get; set; }
}