using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class BulkSubsidiaryProcessorTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
    }

    [TestMethod]
    public async Task ShouldCreateRelationshipsWhereSubsidiaryExistsInRpd()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var parent = fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = fixture.Create<OrganisationResponseModel>();
        var subsidiaries = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();
        var subsidiaryOrganisations = fixture.CreateMany<OrganisationResponseModel>(2).ToArray();
        var notificationServiceMock = new Mock<INotificationService>();

        var subsidiaryService = new Mock<ISubsidiaryService>();
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[0]);
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[1].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[1]);

        var updates = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => updates.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();

        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        updates.Should().HaveCount(2);

        updates.Should().Contain(model => model.UserId == userId &&
                                  model.ParentOrganisationId == parentOrganisation.referenceNumber &&
                                  model.ChildOrganisationId == subsidiaryOrganisations[0].referenceNumber &&
                                  model.ParentOrganisationExternalId == parentOrganisation.ExternalId &&
                                  model.ChildOrganisationExternalId == subsidiaryOrganisations[0].ExternalId);

        updates.Should().Contain(model => model.UserId == userId &&
                                  model.ParentOrganisationId == parentOrganisation.referenceNumber &&
                                  model.ChildOrganisationId == subsidiaryOrganisations[1].referenceNumber &&
                                  model.ParentOrganisationExternalId == parentOrganisation.ExternalId &&
                                  model.ChildOrganisationExternalId == subsidiaryOrganisations[1].ExternalId);
    }

    [TestMethod]
    public async Task ShouldAddOrganisationsWhereRelationshipsDoNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var parent = fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = fixture.Create<OrganisationResponseModel>();
        var subsidiaries = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        var subsidiaryService = new Mock<ISubsidiaryService>();
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync((OrganisationResponseModel)null);

        var inserts = new List<LinkOrganisationModel>();
        subsidiaryService.Setup(ss => ss.CreateAndAddSubsidiaryAsync(It.IsAny<LinkOrganisationModel>()))
            .Callback<LinkOrganisationModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(chdp => chdp.SetCompaniesHouseData(It.IsAny<OrganisationModel>())).ReturnsAsync(true);

        var notificationService = new Mock<INotificationService>();

        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationService.Object);

        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        inserts.Should().HaveCount(2);

        inserts.Should().Contain(insert => insert.UserId == userId &&
                            insert.ParentOrganisationId == parentOrganisation.ExternalId &&
                            insert.Subsidiary.ReferenceNumber == subsidiaries[0].organisation_id &&
                            insert.Subsidiary.Name == subsidiaries[0].organisation_name &&
                            insert.Subsidiary.CompaniesHouseNumber == subsidiaries[0].companies_house_number &&
                            insert.Subsidiary.OrganisationType == OrganisationType.NotSet &&
                            insert.Subsidiary.ProducerType == ProducerType.Other);

        inserts.Should().Contain(insert => insert.UserId == userId &&
                            insert.ParentOrganisationId == parentOrganisation.ExternalId &&
                            insert.Subsidiary.ReferenceNumber == subsidiaries[1].organisation_id &&
                            insert.Subsidiary.Name == subsidiaries[1].organisation_name &&
                            insert.Subsidiary.CompaniesHouseNumber == subsidiaries[1].companies_house_number &&
                            insert.Subsidiary.OrganisationType == OrganisationType.NotSet &&
                            insert.Subsidiary.ProducerType == ProducerType.Other);
    }
}
