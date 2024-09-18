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
    public async Task Should_Process_Organisations()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => null)
            .With(c => c.UploadFileErrorModel, () => null)
            .CreateMany<CompaniesHouseCompany>();

        var parentAndSubsidiaries = _fixture.CreateMany<ParentAndSubsidiaries>();
        var orgModel = _fixture.Create<OrganisationResponseModel>();

        _recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(companyData)).Returns(parentAndSubsidiaries);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(orgModel);

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

         // Act
        await orchestrator.Orchestrate(companyData, userRequestModel);

        // Assert
        foreach (var set in parentAndSubsidiaries)
        {
            _bulkSubsidiaryProcessor.Verify(cp => cp.Process(set.Subsidiaries, set.Parent, orgModel, userRequestModel));
        }
    }

    [TestMethod]
    public async Task Should_Notify_Errors()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => "Test Error")
            .With(c => c.UploadFileErrorModel, () => new UploadFileErrorModel { IsError = true, FileLineNumber = 2, FileContent = "test,test", Message = "Test error" })
            .CreateMany(1);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        // Act
        orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Once());
    }

    [TestMethod]
    public async Task Should_Notify_Errors1()
    {
        var companyData = new List<CompaniesHouseCompany>();
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        // Act
        orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Once());
    }

    [TestMethod]
    public async Task Should_Notify_Errors_When_No_Errors()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => null)
            .With(c => c.UploadFileErrorModel, () => null)
            .CreateMany(2);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object);

        // Act
        orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Never());
    }
}