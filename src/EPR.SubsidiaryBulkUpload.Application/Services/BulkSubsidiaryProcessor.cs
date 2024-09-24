using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkSubsidiaryProcessor(ISubsidiaryService organisationService, ICompaniesHouseDataProvider companiesHouseDataProvider, ILogger<BulkSubsidiaryProcessor> logger, INotificationService notificationService)
    : IBulkSubsidiaryProcessor
{
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

        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg
            .Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg?.companiesHouseNumber && sub.Subsidiary.organisation_name != sub.SubsidiaryOrg?.name);
        var subWithInvalidName = await subsidiariesAndOrgWith_InValidName.Select(s => s.Subsidiary).ToListAsync();

        string message = "The subsidiary company house number is in RPD, but the name is different\r\n Note, could this be because the company name has changed.";
        /*Scenario 2: The subsidiary found in companies house. name not match*/
        await ReportCompanies(subWithInvalidName, userRequestModel, message);

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

        /*Scenario 1: The subsidiary is not found in RPD and not in Local storage and not found on companies house*/
        message = "Subsidiaries not found in RPD and not in Local storage and also not found on companies house.";
        await ReportCompanies(subsidiariesNotAdded, userRequestModel, message);
    }

    private async Task ReportCompanies(IEnumerable<CompaniesHouseCompany> subsidiaries, UserRequestModel userRequestModel, string message)
    {
        var notificationErrorList = new List<UploadFileErrorModel>();
        foreach (var company in subsidiaries)
        {
            var newError = new UploadFileErrorModel()
            {
                FileContent = company.organisation_name + "-" + company.companies_house_number,
                Message = message,
                IsError = true,
                FileLineNumber = company.FileLineNumber
            };

            notificationErrorList.Add(newError);
        }

        var key = userRequestModel.GenerateKey(BulkUpdateErrors.SubsidiaryBulkUploadProgress);
        var keyErrors = userRequestModel.GenerateKey(BulkUpdateErrors.SubsidiaryBulkUploadErrors);
        _notificationService.SetStatus(key, "Started reporting invalid subsidiaries.");
        _notificationService.SetErrorStatus(keyErrors, notificationErrorList);
        _logger.LogInformation(message);
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