using System.Diagnostics.CodeAnalysis;
using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class LinkOrganisationModel
{
    public OrganisationModel Subsidiary { get; set; }

    public Guid ParentOrganisationId { get; set; }

    public Guid? UserId { get; set; }

    public HttpStatusCode? StatusCode { get; set; }
}
