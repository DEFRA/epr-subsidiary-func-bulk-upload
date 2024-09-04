using System.Diagnostics.CodeAnalysis;
using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class Company
{
    public Company()
    {
    }

    public Company(CompaniesHouseCompany? organisation)
        : this()
    {
        if (organisation == null)
        {
            throw new ArgumentException("Organisation cannot be null.");
        }

        CompaniesHouseNumber = organisation.Organisation?.RegistrationNumber ?? string.Empty;
        Name = organisation.Organisation?.Name ?? string.Empty;
        BusinessAddress = new Address(organisation.Organisation?.RegisteredOffice);
        AccountCreatedOn = organisation.AccountCreatedOn;
    }

    public Company(CompaniesHouseResponse? organisation)
        : this()
    {
        if (organisation == null)
        {
            throw new ArgumentException("Organisation cannot be null.");
        }

        CompaniesHouseNumber = organisation.Organisation?.RegistrationNumber ?? string.Empty;
        Name = organisation.Organisation?.Name ?? string.Empty;
        BusinessAddress = new Address(organisation.Organisation?.RegisteredOffice);
        AccountCreatedOn = organisation.AccountCreatedOn;
    }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public DateTimeOffset? AccountCreatedOn { get; set; }

    public Address BusinessAddress { get; set; }
}