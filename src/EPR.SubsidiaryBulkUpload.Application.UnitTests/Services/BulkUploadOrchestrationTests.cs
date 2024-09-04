using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class BulkUploadOrchestrationTests
{
    private Fixture _fixture;
    private Mock<IRecordExtraction> _recordExtraction;
    private Mock<ISubsidiaryService> _subsidiaryService;
    private Mock<IBulkSubsidiaryProcessor> _bulkSubsidiaryProcessor;
    private Mock<INotificationService> _notificationService;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _recordExtraction = new Mock<IRecordExtraction>();
        _subsidiaryService = new Mock<ISubsidiaryService>();
        _bulkSubsidiaryProcessor = new Mock<IBulkSubsidiaryProcessor>();
        _notificationService = new Mock<INotificationService>();
    }

    [TestMethod]
    public async Task ShouldProcessOrganisations()
    {
        // Arrange
        var companyData = _fixture.CreateMany<CompaniesHouseCompany>();
        var parentAndSubsidiaries = _fixture.CreateMany<ParentAndSubsidiaries>();
        var orgModel = _fixture.Create<OrganisationResponseModel>();

        _recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(companyData)).Returns(parentAndSubsidiaries);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(orgModel);

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        // Act
        await orchestrator.Orchestrate(companyData, new UserRequestModel { UserId = userId, OrganisationId = organisationId });

        // Assert
        foreach (var set in parentAndSubsidiaries)
        {
            _bulkSubsidiaryProcessor.Verify(cp => cp.Process(set.Subsidiaries, set.Parent, orgModel, userId));
        }
    }

    [TestMethod]
    public async Task NotifyErrors()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => "Test Error")
            .With(c => c.UploadFileErrorModel, () => new UploadFileErrorModel { IsError = true, FileLineNumber = 2, FileContent = "test,test", Message = "Test error" })
            .CreateMany(1);

        // var apiResponse = _fixture.Build<CompaniesHouseResponseFromCompaniesHouse>().With(x => x.date_of_creation, () => DateTime.Today.ToString("yyyy-MM-dd")).Create();
        // var parentAndSubsidiaries = fixture.CreateMany<ParentAndSubsidiaries>();
        // var orgModel = fixture.Create<OrganisationResponseModel>();
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        // Act
        orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Once());
    }
}