﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Options;

[ExcludeFromCodeCoverage]
public class ApiOptions : ApiResilienceOptions
{
    public const string SectionName = "ApiConfig";

    public string CompaniesHouseLookupBaseUrl { get; set; } = null!;

    public string CompaniesHouseDirectBaseUri { get; set; }

    public bool UseDirectCompaniesHouseLookup { get; set; }

    public string CompaniesHouseDirectApiKey { get; set; }

    public string CompaniesHouseScope { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string TenantId { get; set; }

    public int Timeout { get; set; }

    public string AccountServiceClientId { get; set; } = null!;

    public string SubsidiaryServiceBaseUrl { get; set; } = null!;
}
