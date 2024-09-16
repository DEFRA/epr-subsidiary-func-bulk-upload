using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkSubsidiaryProcessor(ISubsidiaryService organisationService, ICompaniesHouseDataProvider companiesHouseDataProvider, ILogger<BulkSubsidiaryProcessor> logger, INotificationService notificationService)
    : IBulkSubsidiaryProcessor
{
    private const string SubsidiaryBulkUploadMismatchedProgress = "Subsidiary bulk upload progress";
    private const string SubsidiaryBulkUploadMismatchedErrors = "Subsidiary bulk upload errors. Mismatched subsidiaries.";
    private readonly ILogger<BulkSubsidiaryProcessor> _logger = logger;
    private readonly ISubsidiaryService organisationService = organisationService;
    private readonly ICompaniesHouseDataProvider companiesHouseDataProvider = companiesHouseDataProvider;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Process(IEnumerable<CompaniesHouseCompany> subsidiaries, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg, UserRequestModel userRequestModel)
    {
        var subsidiariesAndOrg = subsidiaries
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.companies_house_number)));

        var subsidiariesAndOrgWithValidName = subsidiariesAndOrg
            .Where(sub => sub.SubsidiaryOrg != null && sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber && sub.Subsidiary.organisation_name == sub.SubsidiaryOrg.name);

        var knownSubsidiariesToAdd = subsidiariesAndOrgWithValidName.Where(co => co.SubsidiaryOrg != null)
            .SelectAwait(async co =>
                (Subsidiary: co.Subsidiary,
                 SubsidiaryOrg: co.SubsidiaryOrg,
                 RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id)))
            .Where(co => !co.RelationshipExists);

        await foreach (var subsidiaryAddModel in knownSubsidiariesToAdd)
        {
            await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userRequestModel.UserId, subsidiaryAddModel.Subsidiary);
        }

        /*Scenario 2:
        The subsidiary companies house number is in RPD, but the name is different
        Note, could this be because the company name has changed.What do we do then?
        When a subsidiary is found in RPD
        But the name for the subsidiary in cvs file is different to the name in RPD
        Then an error is created for that row in the bulk upload file.
        Implementation: build another collection of those subsidiaries where csv file name not matching with RPD name and
        report to Redis
         */
        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg
            .Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber && sub.Subsidiary.organisation_name != sub.SubsidiaryOrg?.name);
        var subWithInvalidName = await subsidiariesAndOrgWith_InValidName.Select(s => s.Subsidiary).ToListAsync();

        string message = "The subsidiary company house number is in RPD, but the name is different\r\n Note, could this be because the company name has changed.";
        string errorMesssage = "Mismatched named subsidiaries found. Subsidiary reported : {Count}";
        /*Scenario 2:
        The subsidiary found in companies house. name not match*/
        await ReportCompanies(subWithInvalidName, userRequestModel, message, errorMesssage);

        var newSubsidiariesToAdd = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
            .SelectAwait(async subsidiary =>
                (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userRequestModel.UserId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        await foreach (var subsidiaryandLink in newSubsidiariesToAdd)
        {
            subsidiaryandLink.LinkModel.StatusCode = await organisationService.CreateAndAddSubsidiaryAsync(subsidiaryandLink.LinkModel);
        }

        var allAdded = await newSubsidiariesToAdd.Where(sta => sta.LinkModel.StatusCode == System.Net.HttpStatusCode.OK).Select(sta => sta.Subsidiary)
            .Concat(subsidiariesAndOrgWithValidName.Select(swoan => swoan.Subsidiary))
            .ToListAsync();

        var subsidiariesNotAdded = subsidiaries.Except(allAdded);

        /*Scenario 1:
        The subsidiary is not found in RPD and not in Local storage and not found on companies house*/
        message = "Subsidiaries not found in RPD and not in Local storage and also not found on companies house.";
        errorMesssage = "Subsidiaries not  found in RPD, local storage and companies house database. Subsidiary reported : {Count}";
        await ReportCompanies(subsidiariesNotAdded, userRequestModel, message, errorMesssage);
    }

    private async Task ReportCompanies(IEnumerable<CompaniesHouseCompany> subsidiaries, UserRequestModel userRequestModel, string message, string logMessage)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in subsidiaries)
        {
            var newError = new UploadFileErrorModel()
            {
                FileContent = company.organisation_name + "-" + company.companies_house_number,
                Message = message
            };

            notificationErrorList.Add(newError);
        }

        var key = userRequestModel.GenerateKey(SubsidiaryBulkUploadMismatchedProgress);
        var keyErrors = userRequestModel.GenerateKey(SubsidiaryBulkUploadMismatchedErrors);
        _notificationService.SetStatus(key, "Started reporting invalid subsidiaries.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation(logMessage, subsidiaries.Count().ToString());
    }

    private async Task<LinkOrganisationModel?> GetLinkModelForCompaniesHouseData(CompaniesHouseCompany subsidiary, OrganisationResponseModel parentOrg, Guid userId)
    {
        var newSubsidiaryModel = new LinkOrganisationModel()
        {
            UserId = userId,
            Subsidiary = new OrganisationModel()
            {
                ReferenceNumber = subsidiary.organisation_id,
                Name = subsidiary.organisation_name,
                CompaniesHouseNumber = subsidiary.companies_house_number,
                OrganisationType = OrganisationType.NotSet,
                ProducerType = ProducerType.Other,
                IsComplianceScheme = false,
                Nation = Nation.NotSet,
                SubsidiaryOrganisationId = subsidiary.subsidiary_id
            },
            ParentOrganisationId = parentOrg.ExternalId.Value
        };

        var modelLoaded = await companiesHouseDataProvider.SetCompaniesHouseData(newSubsidiaryModel.Subsidiary);

        return modelLoaded ? newSubsidiaryModel : null;
    }

    private async Task AddSubsidiary(OrganisationResponseModel parent, OrganisationResponseModel subsidiary, Guid userId, CompaniesHouseCompany subsidiaryFileData)
    {
        var subsidiaryModel = new SubsidiaryAddModel
        {
            UserId = userId,
            ParentOrganisationId = parent.referenceNumber,
            ChildOrganisationId = subsidiary.referenceNumber,
            ParentOrganisationExternalId = parent.ExternalId,
            ChildOrganisationExternalId = subsidiary.ExternalId
        };
        await organisationService.AddSubsidiaryRelationshipAsync(subsidiaryModel);

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);
    }
}