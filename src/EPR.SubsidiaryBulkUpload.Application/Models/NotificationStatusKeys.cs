﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public static class NotificationStatusKeys
{
    public const string SubsidiaryBulkUploadProgress = "Subsidiary bulk upload progress";

    public const string SubsidiaryBulkUploadErrors = "Subsidiary bulk upload errors";

    public const string SubsidiaryBulkUploadRowsAdded = "Subsidiary bulk upload rows added";
}