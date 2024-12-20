﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class UserRequestModel
{
    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }
}
