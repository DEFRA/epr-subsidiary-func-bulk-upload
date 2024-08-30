using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class BulkSubsidiaryProcessor(ISubsidiaryService organisationService, ICompaniesHouseDataProvider companiesHouseDataProvider, ILogger<BulkSubsidiaryProcessor> logger)
    : IBulkSubsidiaryProcessor
{
    private readonly ILogger<BulkSubsidiaryProcessor> _logger = logger;
    private readonly ISubsidiaryService organisationService = organisationService;
    private readonly ICompaniesHouseDataProvider companiesHouseDataProvider = companiesHouseDataProvider;

    public async Task Process(IEnumerable<CompaniesHouseCompany> subsidiaries, CompaniesHouseCompany parent, OrganisationResponseModel parentOrg, Guid userId)
    {
        var subsidiariesAndOrg = subsidiaries
            .ToAsyncEnumerable()
            .SelectAwait(async subsidiary => (Subsidiary: subsidiary, SubsidiaryOrg: await organisationService.GetCompanyByCompaniesHouseNumber(subsidiary.companies_house_number)));

        // All subsidiaries with an org id, where no relationship already exists
        var knownSubsidiariesToAdd = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg != null)
            .SelectAwait(async co =>
                (Subsidiary: co.Subsidiary,
                 SubsidiaryOrg: co.SubsidiaryOrg,
                 RelationshipExists: await organisationService.GetSubsidiaryRelationshipAsync(parentOrg.id, co.SubsidiaryOrg.id)))
            .Where(co => !co.RelationshipExists);

        // Add relationships for for the children already in RPD...
        await foreach (var subsidiaryAddModel in knownSubsidiariesToAdd)
        {
            await AddSubsidiary(parentOrg, subsidiaryAddModel!.SubsidiaryOrg, userId);
        }

        // Subsidiaries which do not exist in the RPD
        var newSubsidiariesToAdd = subsidiariesAndOrg.Where(co => co.SubsidiaryOrg == null)
            .SelectAwait(async subsidiary =>
                (Subsidiary: subsidiary.Subsidiary, LinkModel: await GetLinkModelForCompaniesHouseData(subsidiary.Subsidiary, parentOrg, userId)))
            .Where(subAndLink => subAndLink.LinkModel != null);

        // Create and add subsidiaries where the companies house data has been provided
        await foreach(var subsidiaryandLink in newSubsidiariesToAdd)
        {
            await organisationService.CreateAndAddSubsidiaryAsync(subsidiaryandLink.LinkModel);
        }
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
