using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.DTOs;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCompany
{
    public string Organisation_Id { get; set; }

    public string Subsidiary_Id { get; set; }

    public string Organisation_Name { get; set; }

    public string Companies_House_Number { get; set; }

    public string Parent_child { get; set; }

    public string Franchisee_licensee_tenant { get; set; }

    // public Company(CompaniesHouseCompany? organisation)
    // : this()
    // {
    //    if (organisation == null)
    //    {
    //        throw new ArgumentException("Organisation cannot be null.");
    //    }

    // CompaniesHouseNumber = organisation.Organisation?.RegistrationNumber ?? string.Empty;
    //    Name = organisation.Organisation?.Name ?? string.Empty;
    //    BusinessAddress = new Addresses.Address(organisation.Organisation?.RegisteredOffice);
    //    AccountCreatedOn = organisation.AccountCreatedOn;
    // }
    public string CompanyName { get; set; }

    public string CompanyNumber { get; set; }

    public string RegAddressCareOf { get; set; }

    public int? RegAddressPOBox { get; set; }

    public string RegAddressAddressLine1 { get; set; }

    public string RegAddressAddressLine2 { get; set; }

    public string RegAddressPostTown { get; set; }

    public string RegAddressCounty { get; set; }

    public string RegAddressCountry { get; set; }

    public string RegAddressPostCode { get; set; }

    public string CompanyCategory { get; set; }

    public string CompanyStatus { get; set; }

    public string CountryOfOrigin { get; set; }

    public string DissolutionDate { get; set; }

    public string IncorporationDate { get; set; }

    public int AccountsAccountRefDay { get; set; }

    public int AccountsAccountRefMonth { get; set; }

    public string AccountsNextDueDate { get; set; }

    public string AccountsLastMadeUpDate { get; set; }

    public string AccountsAccountCategory { get; set; }

    public string ReturnsNextDueDate { get; set; }

    public string ReturnsLastMadeUpDate { get; set; }

    public int MortgagesNumMortCharges { get; set; }

    public int MortgagesNumMortOutstanding { get; set; }

    public int MortgagesNumMortPartSatisfied { get; set; }

    public int MortgagesNumMortSatisfied { get; set; }

    public string SICCodeSicText_1 { get; set; }

    public string SICCodeSicText_2 { get; set; }

    public string SICCodeSicText_3 { get; set; }

    public string SICCodeSicText_4 { get; set; }

    public int LimitedPartnershipsNumGenPartners { get; set; }

    public int LimitedPartnershipsNumLimPartners { get; set; }

    public string URI { get; set; }

    public string PreviousName_1CONDATE { get; set; }

    public string PreviousName_1CompanyName { get; set; }

    public string PreviousName_2CONDATE { get; set; }

    public string PreviousName_2CompanyName { get; set; }

    public string PreviousName_3CONDATE { get; set; }

    public string PreviousName_3CompanyName { get; set; }

    public string PreviousName_4CONDATE { get; set; }

    public string PreviousName_4CompanyName { get; set; }

    public string PreviousName_5CONDATE { get; set; }

    public string PreviousName_5CompanyName { get; set; }

    public string PreviousName_6CONDATE { get; set; }

    public string PreviousName_6CompanyName { get; set; }

    public string PreviousName_7CONDATE { get; set; }

    public string PreviousName_7CompanyName { get; set; }

    public string PreviousName_8CONDATE { get; set; }

    public string PreviousName_8CompanyName { get; set; }

    public string PreviousName_9CONDATE { get; set; }

    public string PreviousName_9CompanyName { get; set; }

    public string PreviousName_10CONDATE { get; set; }

    public string PreviousName_10CompanyName { get; set; }

    public string ConfStmtNextDueDate { get; set; }

    public string ConfStmtLastMadeUpDate { get; set; }
}