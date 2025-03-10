﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class SubsidiaryAddModel
{
    public string? ParentOrganisationId { get; init; }

    public string? ChildOrganisationId { get; init; }

    public Guid? ParentOrganisationExternalId { get; init; }

    public Guid? ChildOrganisationExternalId { get; init; }

    public Guid? UserId { get; set; }

    public string? SubsidiaryOrganisationId { get; set; }

    public string? JoinerDate { get; set; }

    public int? ReportingTypeId { get; set; }
}
