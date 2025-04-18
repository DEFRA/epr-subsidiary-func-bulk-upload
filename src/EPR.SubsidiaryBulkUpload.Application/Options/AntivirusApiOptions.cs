﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class AntivirusApiOptions : ApiResilienceOptions
{
    public const string SectionName = "AntivirusApi";

    public string BaseUrl { get; set; }

    public string SubscriptionKey { get; set; }

    public string TenantId { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Scope { get; set; }

    public int Timeout { get; set; }

    public string CollectionSuffix { get; set; }

    public string NotificationEmail { get; set; }
}