namespace EPR.SubsidiaryBulkUpload.Application.Models;

public static class BulkUpdateErrors
{
    public const int FileEmptyError = 100;

    public const int InvalidHeader = 101;

    public const int OrganisationIdRequired = 102;
    public const string OrganisationIdRequiredMessage = "organisation_id is required.";

    public const int OrganisationNameRequired = 103;
    public const string OrganisationNameRequiredMessage = "organisation_name is required.";

    public const int CompaniewsHouseNumberRequired = 104;
    public const string CompaniewsHouseNumberRequiredMessage = "companies_house_number is required.";

    public const int ParentOrChildRequired = 105;
    public const string ParentOrChildRequiredMessage = "parent_or_child is required.";

    public const int FranchiseeLicenseeTenantInvalid = 106;
    public const string FranchiseeLicenseeTenantInvalidMessage = "franchisee_licensee_tenant can only be blank or Yes or Y.";
}
