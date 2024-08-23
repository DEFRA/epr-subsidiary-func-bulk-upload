using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class BulkUploadOrchestrationTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
    }

    [TestMethod]
    public async Task ShouldProcessOrganisations()
    {
        // Arrange
        var companyData = fixture.CreateMany<CompaniesHouseCompany>();
        var parentAndSubsidiaries = fixture.CreateMany<ParentAndSubsidiaries>();
        var orgModel = fixture.Create<OrganisationResponseModel>();

        var recordExtraction = new Mock<IRecordExtraction>();
        var subsidiaryService = new Mock<ISubsidiaryService>();
        var bulkSubsidiaryProcessor = new Mock<IBulkSubsidiaryProcessor>();
        var notificationService = new Mock<INotificationService>();

        recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(companyData)).Returns(parentAndSubsidiaries);

        subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(orgModel);

        var orchestrator = new BulkUploadOrchestration(recordExtraction.Object, subsidiaryService.Object, bulkSubsidiaryProcessor.Object, notificationService.Object);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        // Act
        await orchestrator.Orchestrate(companyData, new UserRequestModel { UserId = userId, OrganisationId = organisationId });

        // Assert
        foreach (var set in parentAndSubsidiaries)
        {
            bulkSubsidiaryProcessor.Verify(cp => cp.Process(set.Subsidiaries, set.Parent, orgModel, userId));
        }
    }
}
