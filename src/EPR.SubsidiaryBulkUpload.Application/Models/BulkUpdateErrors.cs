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
    public const string OrganisationNameRequiredMessage = "The 'organisation name' column is missing.";

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

    public const int CompanyNameNotFoundAnywhere = 111;
    public const string CompanyNameNotFoundAnywhereMessage = "The company name does not match the one on Companies House. Check your company name and try again.";

    public const int FileIsInvalidNoHeaderNoData = 115;
    public const string FileIsInvalidNoHeaderNoDataMessage = "The file is empty. It does not contain headers or data rows.";

    public const int FileIsInvalidWithExtraHeaders = 116;
    public const string FileIsInvalidWithExtraHeadersMessage = "The file has additional column headers: The file has too many column headers. Remove these and try again.";

    public const int InvalidDataFoundInRow = 117;
    public const string InvalidDataFoundInRowMessage = "There is too much column information in the file. Remove this and try again.";

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

    public const int ParentOrganisationIsNotFound = 123;
    public const string ParentOrganisationIsNotFoundErrorMessage = "Parent organisation is not found.";

    public const int ParentOrganisationFoundCompaniesHouseNumberNotMatching = 124;
    public const string ParentOrganisationFoundCompaniesHouseNumberNotMatchingMessage = "Parent companies house number is not correct for this organisation.";

    public const int ParentOrganisationNotValidChildCannotBeProcessed = 125;
    public const string ParentOrganisationNotValidChildCannotBeProcessedErrorMessage = "The parent Organisation is not valid. Child cannot be processed.";

    public const int ParentOrganisationWithNoChildError = 126;
    public const string ParentOrganisationWithNoChildErrorMessage = "There must be at least one child in the subsidiary file.";

    public const int OrphanRecordParentOrganisationIsNotFound = 127;
    public const string OrphanRecordParentOrganisationIsNotFoundErrorMessage = "Orphan Child. Parent organisation is not found.";

    public const int DuplicateRecordsError = 128;
    public const string DuplicateRecordsErrorMessage = "There are two or more lines with duplicate information. Check file and try again.";

    public const int OrganisationIdIsForAnotherOrganisation = 129;
    public const string OrganisationIdIsForAnotherOrganisationMessage = "The organisation id is for another organisation";

    public const int JoinerDateRequired = 130;
    public const string JoinerDateRequiredMessage = "The 'joiner date' column is missing.";

    public const int ReportingTypeRequired = 131;
    public const string ReportingTypeRequiredMessage = "The 'reporting type' column is missing.";

    public const int ReportingTypeValidValueCheck = 132;
    public const string ReportingTypeValidValueCheckMessage = "The 'reporting type' column only allowed 'GROUP' or 'SELF'.";

    public const int JointerDateFormatIncorrect = 133;
    public const string JointerDateFormatIncorrectMessage = "The 'jointer date ' column only allowed British Date Format 'DD/MM/YYYY'.";
}