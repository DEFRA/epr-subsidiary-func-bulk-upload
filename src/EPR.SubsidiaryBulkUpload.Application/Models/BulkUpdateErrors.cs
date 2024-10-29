using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;
[ExcludeFromCodeCoverage]
public static class BulkUpdateErrors
{
    public const int FileEmptyError = 100;
    public const string FileHasNoRecord = "The file you've uploaded does not have any information in it.";

    public const int InvalidHeader = 101;
    public const string InvalidHeaderOrMissingHeadersMessage = "The headers are missing.";

    public const int OrganisationIdRequired = 102;
    public const string OrganisationIdRequiredMessage = "The 'organisation id' column is missing.";

    public const int OrganisationNameRequired = 103;
    public const string OrganisationNameRequiredMessage = "The 'organisation name' column is missing. ";

    public const int CompaniesHouseNumberRequired = 104;
    public const string CompaniesHouseNumberRequiredMessage = "The 'companies house number' column is missing.";

    public const int ParentOrChildRequired = 105;
    public const string ParentOrChildRequiredMessage = "The 'parent or child' column is missing.";

    public const int FranchiseeLicenseeTenantInvalid = 106;
    public const string FranchiseeLicenseeTenantInvalidMessage = "You can only enter 'Y' to the 'franchisee licensee tenant' column, or leave it blank.";

    public const int CompaniesAlreadyBelongsToADifferentParent = 107;
    public const string CompaniesAlreadyBelongsToADifferentParentMessage = "This subsidiary has a different parent.";

    public const int CompanyNameIsDifferentInRPD = 109;
    public const string CompanyNameIsDifferentInRPDMessage = "The company name does not match the one on record. Check company name and try again.";

    public const int CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPI = 110;
    public const string CompanyNameIsDifferentInOfflineDataAndDifferentInCHAPIMessage = "The company name does not match the one on record. Check your company name and try again.";

    public const int CompanyNameNofoundAnywhere = 111;
    public const string CompanyNameNofoundAnywhereMessage = "The company name does not match the one on Companies House. Check your company name and try again.";

    // 112 Company Name not found in RPD(backend storage)     The company name is not found in Report Packaging Data. Check your company name and try again.
    // 113 Company Name not found in Offline Table Storage The company name is not found in our records. Check the company name and try again.
    // 114 Company Name not found in Companies House database The Company name is not found in the Companies House database. Check the name and try again.
    public const int FileisInvalidNoHeaderNoData = 115;
    public const string FileisInvalidNoHeaderNoDataMessage = "The file is empty. It does not contain headers or data rows.";

    public const int FileisInvalidWithExtraHeaders = 116;
    public const string FileisInvalidWithExtraHeadersMessage = "The file has additional column headers: The file has too many column headers. Remove these and try again.";

    public const int InvalidDatafoundinRow = 117;
    public const string InvalidDatafoundinRowMessage = "There is too much column information in the file. Remove this and try again.";

    public const int ResourceNotFoundError = 118;
    public const string ResourceNotFoundErrorMessage = "Information cannot be retrieved. Try again later.";

    public const int ResourceNotReachableError = 119;
    public const string ResourceNotReachableErrorMessage = "There is a problem with the service. Try again later.";

    public const int InvalidCompaniesHouseNumberLengthError = 120;
    public const string InvalidCompaniesHouseNumberLengthErrorMessage = "Your Companies House number must be 8 characters or fewer.";

    public const int ResourceNotReachableOrAllOtherPossibleError = 121;
    public const string ResourceNotReachableOrAllOtherPossibleErrorMessage = "Unexpected error when retrieving data from Companies House. Try again later.";

    public const int SpacesInCompaniesHouseNumberError = 122;
    public const string SpacesInCompaniesHouseNumberErrorMessage = "Spaces in Companies House Number not allowed. Invalid Number.";
}