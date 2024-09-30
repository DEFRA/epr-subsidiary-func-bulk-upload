namespace EPR.SubsidiaryBulkUpload.Application.Models;

public static class BulkUpdateErrors
{
    public const int FileEmptyError = 100;
    public const int InvalidHeader = 101;

    public const int OrganisationIdRequired = 102;
    public const string OrganisationIdRequiredMessage = "organisation_id is required.";

    public const int OrganisationNameRequired = 103;
    public const string OrganisationNameRequiredMessage = "organisation_name is required.";

    public const int CompaniesHouseNumberRequired = 104;
    public const string CompaniesHouseNumberRequiredMessage = "companies_house_number is required.";

    public const int ParentOrChildRequired = 105;
    public const string ParentOrChildRequiredMessage = "parent_or_child is required.";

    public const int FranchiseeLicenseeTenantInvalid = 106;
    public const string FranchiseeLicenseeTenantInvalidMessage = "franchisee_licensee_tenant can only be blank or Yes or Y.";

    public const int CompaniesAlreadyBelongsToADifferentParent = 107;
    public const string CompaniesAlreadyBelongsToADifferentParentMessage = "Invalid Request. Subsidiary already has a different parent.";

    public const int CompanyNameIsDifferentInRPD = 109;
    public const string CompanyNameIsDifferentInRPDMessage = "Company Name is different in RPD.";

    public const int CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPI = 110;
    public const string CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPIMessage = "Company Name is different in Offline Data and CH Data.";

    public const int CompanyNameNofoundAnywhere = 111;
    public const string CompanyNameNofoundAnywhereMessage = "Company Name not found in RPD, Offline Data and Companies house API data.";

    public const int FileisInvalidNoHeaderNoData = 112;
    public const int FileisInvalidWithExtraHeaders = 113;
}