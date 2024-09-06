using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading.Channels;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkSubsidiaryProcessor(ISubsidiaryService organisationService, ICompaniesHouseDataProvider companiesHouseDataProvider, ILogger<BulkSubsidiaryProcessor> logger)
    : IBulkSubsidiaryProcessor
{
    private readonly ILogger<BulkSubsidiaryProcessor> _logger = logger;
    private readonly ISubsidiaryService organisationService = organisationService;
    private readonly ICompaniesHouseDataProvider companiesHouseDataProvider = companiesHouseDataProvider;

    public async Task Process(IEnumerable<CompaniesHouseCompany> subsidiaries, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg, Guid userId)
    {
        // these are the subsidiaries which exists in RPD
        var subsidiariesAndOrg = subsidiaries
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.companies_house_number)));



        // before going further. here we can check with RPD if the company name in the file is matching the RPD Org table company name
        var subsidiariesAndOrgWithValidName = subsidiariesAndOrg
            .Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber && sub.Subsidiary.organisation_name == sub.SubsidiaryOrg.name);

        // after checking the name only that collection will pass on for relationship check

        // All subsidiaries with an org id, where no relationship already exists and Company name in RPD mathes with company name in file
        var knownSubsidiariesToAdd = subsidiariesAndOrgWithValidName.Where(co => co.SubsidiaryOrg != null)
            .SelectAwait(async co =>
                (Subsidiary: co.Subsidiary,
                 SubsidiaryOrg: co.SubsidiaryOrg,
                 RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id)))
            .Where(co => !co.RelationshipExists);

        // Add relationships for the children already in RPD...
        await foreach (var subsidiaryAddModel in knownSubsidiariesToAdd)
        {
            await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userId);
        }

        /*Scenario 2:

        The subsidiary companies house number is in RPD, but the name is different
        Note, could this be because the company name has changed.What do we do then?
        When a subsidiary is found in RPD
        But the name for the subsidiary is different to the name in RPD
        Then an error is created for that row in the bulk upload file.*/

        // build another collection of those subsidiaries where csv file name not matching with RPD name and
        // report to Redis
        var subsidiariesAndOrgWith_InValidName = subsidiariesAndOrg
            .Where(sub => sub.Subsidiary.companies_house_number == sub.SubsidiaryOrg.companiesHouseNumber && sub.Subsidiary.organisation_name != sub.SubsidiaryOrg.name);

        // check and report the remaining ones and raise error for none processed subsidiaries.
        await ReportCompanies((IEnumerable<CompaniesHouseCompany>)subsidiariesAndOrgWith_InValidName);

        // Subsidiaries which do not exist in the RPD
        var newSubsidiariesToAdd = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
            .SelectAwait(async subsidiary =>
                (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        // Create and add subsidiaries where the companies house data has been provided
        await foreach (var subsidiaryandLink in newSubsidiariesToAdd)
        {
            await organisationService.CreateAndAddSubsidiaryAsync(subsidiaryandLink.LinkModel);
        }

        // check and report the remaining ones and raise error for none processed subsidiaries.
        await ReportCompanies1(parentOrg, subsidiaries);
    }

    private async Task ReportCompanies(IEnumerable<CompaniesHouseCompany> subsidiaries)
    {
        /*Scenario 1:
                The subsidiary is not found in companies house*/
        var noneProcessedCompanies = await organisationService.GetNoneProccessedCompanies(subsidiaries);

        var notificationErrorList = new List<UploadFileErrorModel>();

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);
    }

    private async Task ReportCompanies1(OrganisationResponseModel parent, IEnumerable<CompaniesHouseCompany> subsidiaries)
    {
        /* var subsidiaryModel = new SubsidiaryAddModel
         {
             UserId = userId,
             ParentOrganisationId = parent.referenceNumber,
             ChildOrganisationId = subsidiary.referenceNumber,
             ParentOrganisationExternalId = parent.ExternalId,
             ChildOrganisationExternalId = subsidiary.ExternalId
         };
         */

        /*Scenario 1:
                The subsidiary is not found in companies house*/
        var noneProcessedCompanies = await organisationService.GetNoneProccessedCompanies(subsidiaries);

        var notificationErrorList = new List<UploadFileErrorModel>();

        _logger.LogInformation("Subsidiary Company {SubsidiaryReferenceNumber} {SubsidiaryName} linked to {ParentReferenceNumber} in the database.", subsidiary.referenceNumber, subsidiary.name, parent.referenceNumber);
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
                Nation = Nation.NotSet
            },
            ParentOrganisationId = parentOrg.ExternalId.Value
        };

        var modelLoaded = await companiesHouseDataProvider.SetCompaniesHouseData(newSubsidiaryModel.Subsidiary);

        return modelLoaded ? newSubsidiaryModel : null;
    }

    private async Task AddSubsidiary(OrganisationResponseModel parent, OrganisationResponseModel subsidiary, Guid userId)
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
