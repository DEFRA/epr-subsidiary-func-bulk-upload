using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UserOrganisation
{
    public Guid? UserId { get; set; }

    public Guid? OrganisationId { get; set; }
}
