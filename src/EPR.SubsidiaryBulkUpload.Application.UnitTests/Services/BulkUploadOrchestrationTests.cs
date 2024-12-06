using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

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
            .With(c => c.Errors, () => new List<UploadFileErrorModel>())
            .CreateMany<CompaniesHouseCompany>();

        var parentAndSubsidiaries = _fixture.CreateMany<ParentAndSubsidiaries>(2).ToArray();

        var subsidiaries = _fixture.CreateMany<OrganisationResponseModel>(2).ToArray();

        subsidiaries[0].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        subsidiaries[1].companiesHouseNumber = parentAndSubsidiaries[1].Parent.companies_house_number;
        subsidiaries[0].name = parentAndSubsidiaries[0].Parent.organisation_name;
        subsidiaries[1].name = parentAndSubsidiaries[1].Parent.organisation_name;

        _recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(companyData)).Returns(parentAndSubsidiaries);
        _bulkSubsidiaryProcessor.Setup(se => se.Process(It.IsAny<IEnumerable<CompaniesHouseCompany>>(), It.IsAny<CompaniesHouseCompany>(), It.IsAny<OrganisationResponseModel>(), It.IsAny<UserRequestModel>())).ReturnsAsync(1);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

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
        _bulkSubsidiaryProcessor.Verify(cp => cp.Process(It.IsAny<IEnumerable<CompaniesHouseCompany>>(), It.IsAny<CompaniesHouseCompany>(), It.IsAny<OrganisationResponseModel>(), It.IsAny<UserRequestModel>()));
    }

    [TestMethod]
    public async Task Should_Notify_Errors()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1);

        companyData.First().Errors.Add(_fixture.Create<UploadFileErrorModel>());

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        // Act
        await orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Once());
    }

    [TestMethod]
    public async Task Should_Notify_Start()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        // Act
        await orchestrator.NotifyStart(userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task Should_Notify_Errors_On_Empty_File()
    {
        var companyData = new List<CompaniesHouseCompany>();
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        // Act
        await orchestrator.NotifyErrors(companyData, userRequestModel);

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
            .With(c => c.Errors, () => new())
            .CreateMany(2);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        // Act
        await orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Never());
    }

    [TestMethod]
    public async Task Should_Notify_Errors_When_No_Data()
    {
        // Arrange
        var companyData = Array.Empty<CompaniesHouseCompany>();

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel { UserId = userId, OrganisationId = organisationId };

        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        // Act
        await orchestrator.NotifyErrors(companyData, userRequestModel);

        // Assert
        _notificationService.Verify(ns => ns.SetStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _notificationService.Verify(ns => ns.SetErrorStatus(It.IsAny<string>(), It.IsAny<List<UploadFileErrorModel>>()), Times.Once());
    }

    [TestMethod]
    public async Task Should_Process_Duplicate_Organisations()
    {
        // Arrange
        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new List<UploadFileErrorModel>())
            .CreateMany<CompaniesHouseCompany>();

        var parentAndSubsidiaries = _fixture.CreateMany<ParentAndSubsidiaries>(1).ToArray();

        var companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        var organisationName = parentAndSubsidiaries[0].Parent.organisation_name;
        const string child = "child";
        const string orgId = "123";
        parentAndSubsidiaries[0].Subsidiaries[0].companies_house_number = companiesHouseNumber;
        parentAndSubsidiaries[0].Subsidiaries[1].companies_house_number = companiesHouseNumber;
        parentAndSubsidiaries[0].Subsidiaries[2].companies_house_number = companiesHouseNumber;

        parentAndSubsidiaries[0].Subsidiaries[0].organisation_name = organisationName;
        parentAndSubsidiaries[0].Subsidiaries[1].organisation_name = organisationName;
        parentAndSubsidiaries[0].Subsidiaries[2].organisation_name = organisationName;

        parentAndSubsidiaries[0].Subsidiaries[0].parent_child = child;
        parentAndSubsidiaries[0].Subsidiaries[1].parent_child = child;
        parentAndSubsidiaries[0].Subsidiaries[2].parent_child = child;

        parentAndSubsidiaries[0].Subsidiaries[0].organisation_id = orgId;
        parentAndSubsidiaries[0].Subsidiaries[1].organisation_id = orgId;
        parentAndSubsidiaries[0].Subsidiaries[2].organisation_id = orgId;

        var subsidiaries = _fixture.CreateMany<OrganisationResponseModel>(3).ToArray();
        subsidiaries[0].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        subsidiaries[1].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        subsidiaries[2].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;

        subsidiaries[0].name = parentAndSubsidiaries[0].Parent.organisation_name;
        subsidiaries[1].name = parentAndSubsidiaries[0].Parent.organisation_name;
        subsidiaries[2].name = parentAndSubsidiaries[0].Parent.organisation_name;

        _recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(companyData)).Returns(parentAndSubsidiaries);
        _bulkSubsidiaryProcessor.Setup(se => se.Process(It.IsAny<IEnumerable<CompaniesHouseCompany>>(), It.IsAny<CompaniesHouseCompany>(), It.IsAny<OrganisationResponseModel>(), It.IsAny<UserRequestModel>())).ReturnsAsync(1);

        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[2]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[2]);
        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

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
        _bulkSubsidiaryProcessor.Verify(cp => cp.Process(It.Is<IEnumerable<CompaniesHouseCompany>>(companyData => companyData.Count() == 1), It.IsAny<CompaniesHouseCompany>(), It.IsAny<OrganisationResponseModel>(), It.IsAny<UserRequestModel>()));
    }

    [TestMethod]
    public async Task Should_Process_Duplicate_Parent_Organisations()
    {
        var orgaId = "101731";
        const string child = "child";
        const string orgId = "123";

        // Arrange
        var companyData0 = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new List<UploadFileErrorModel>())
            .CreateMany<CompaniesHouseCompany>(1);

        var companyData = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new List<UploadFileErrorModel>())
            .CreateMany<CompaniesHouseCompany>();

        var companyDataChildren = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new List<UploadFileErrorModel>())
            .CreateMany<CompaniesHouseCompany>();

        var combo = companyData.Concat(companyDataChildren).Concat(companyData0);

        var parentAndSubsidiaries = _fixture.CreateMany<ParentAndSubsidiaries>(1).ToArray();

        var companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        var organisationName = parentAndSubsidiaries[0].Parent.organisation_name;

        parentAndSubsidiaries[0].Subsidiaries[0].companies_house_number = companiesHouseNumber;
        parentAndSubsidiaries[0].Subsidiaries[1].companies_house_number = companiesHouseNumber;
        parentAndSubsidiaries[0].Subsidiaries[2].companies_house_number = companiesHouseNumber;

        parentAndSubsidiaries[0].Subsidiaries[0].organisation_name = organisationName;
        parentAndSubsidiaries[0].Subsidiaries[1].organisation_name = organisationName;
        parentAndSubsidiaries[0].Subsidiaries[2].organisation_name = organisationName;

        parentAndSubsidiaries[0].Subsidiaries[0].parent_child = child;
        parentAndSubsidiaries[0].Subsidiaries[1].parent_child = child;
        parentAndSubsidiaries[0].Subsidiaries[2].parent_child = child;

        parentAndSubsidiaries[0].Subsidiaries[0].organisation_id = orgId;
        parentAndSubsidiaries[0].Subsidiaries[1].organisation_id = orgId;
        parentAndSubsidiaries[0].Subsidiaries[2].organisation_id = orgId;

        var subsidiaries = _fixture.CreateMany<OrganisationResponseModel>(3).ToArray();
        subsidiaries[0].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        subsidiaries[1].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;
        subsidiaries[2].companiesHouseNumber = parentAndSubsidiaries[0].Parent.companies_house_number;

        subsidiaries[0].name = parentAndSubsidiaries[0].Parent.organisation_name;
        subsidiaries[1].name = parentAndSubsidiaries[0].Parent.organisation_name;
        subsidiaries[2].name = parentAndSubsidiaries[0].Parent.organisation_name;

        _recordExtraction.Setup(re => re.ExtractParentsAndSubsidiaries(combo)).Returns(parentAndSubsidiaries);
        _bulkSubsidiaryProcessor.Setup(se => se.Process(It.IsAny<IEnumerable<CompaniesHouseCompany>>(), It.IsAny<CompaniesHouseCompany>(), It.IsAny<OrganisationResponseModel>(), It.IsAny<UserRequestModel>())).ReturnsAsync(1);

        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);
        _subsidiaryService.Setup(se => se.GetCompanyByReferenceNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[2]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[0]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[1]);
        _subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(subsidiaries[2]);
        var orchestrator = new BulkUploadOrchestration(_recordExtraction.Object, _subsidiaryService.Object, _bulkSubsidiaryProcessor.Object, _notificationService.Object, NullLogger<BulkUploadOrchestration>.Instance);

        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        foreach (var comp in companyData)
        {
            comp.organisation_id = companyData0.FirstOrDefault().organisation_id;
            comp.parent_child = "parent";
        }

        // Act
        await orchestrator.Orchestrate(combo, userRequestModel);

        // Assert
        _recordExtraction.Verify(cp => cp.ExtractParentsAndSubsidiaries(It.IsAny<IEnumerable<CompaniesHouseCompany>>()), Times.AtLeastOnce);
    }
}