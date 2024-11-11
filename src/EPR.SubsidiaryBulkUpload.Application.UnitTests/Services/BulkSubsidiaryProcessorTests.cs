using System.Net;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class BulkSubsidiaryProcessorTests
{
    private Fixture _fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
    }

    [TestMethod]
    public async Task ShouldCreateRelationshipsWhereSubsidiaryExistsInRpd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();
        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(2).ToArray();
        var notificationServiceMock = new Mock<INotificationService>();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaryOrganisations[1].companiesHouseNumber = subsidiaries[1].companies_house_number;
        subsidiaryOrganisations[1].name = subsidiaries[1].organisation_name;

        var subsidiaryService = new Mock<ISubsidiaryService>();
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[0]);
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[1].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[1]);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<LinkOrganisationModel>();
        subsidiaryService.Setup(ss => ss.CreateAndAddSubsidiaryAsync(It.IsAny<LinkOrganisationModel>()))
            .ReturnsAsync(HttpStatusCode.OK)
            .Callback<LinkOrganisationModel>(model => inserts.Add(model));

        var key = "testKey";
        var errorsModel = new List<UploadFileErrorModel> { new UploadFileErrorModel() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        notificationServiceMock.Setup(ss => ss.SetErrorStatus(key, errorsModel));

        var updates = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => updates.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        // Act
        await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        updates.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ShouldAddOrganisationRelationshipWhereRelationshipsDoNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(2).ToArray();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaries[0].Errors = null;
        subsidiaryOrganisations[1].companiesHouseNumber = subsidiaries[1].companies_house_number;
        subsidiaryOrganisations[1].name = subsidiaries[1].organisation_name;
        subsidiaries[1].Errors = null;

        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[0]);
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[1].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[1]);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(chdp => chdp.SetCompaniesHouseData(It.IsAny<OrganisationModel>())).ReturnsAsync(true);

        var notificationServiceMock = new Mock<INotificationService>();
/*        var key = "testKey";
        var errorsModel = new List<UploadFileErrorModel> { new UploadFileErrorModel() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        notificationServiceMock.Setup(ss => ss.SetErrorStatus(key, errorsModel));*/

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
        inserts.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ShouldLinkSubsidiaryWhenItDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        // Return a null OrganisationResponseModel to simulate the company not existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(chdp => chdp.SetCompaniesHouseData(It.IsAny<OrganisationModel>())).ReturnsAsync(true);

        var notificationServiceMock = new Mock<INotificationService>();
        var key = "testKey";
        var errorsModel = new List<UploadFileErrorModel> { new UploadFileErrorModel() { FileLineNumber = 1, Message = "testMessage", IsError = true } };
        notificationServiceMock.Setup(ss => ss.SetErrorStatus(key, errorsModel));

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
        inserts.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task ShouldNotLinkSubsidiaryWhenAPIDataDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .With(c => c.franchisee_licensee_tenant, () => null)
            .CreateMany(1)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        // Return a null OrganisationResponseModel to simulate the company not existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model => model.Error = new UploadFileErrorModel() { ErrorNumber = 101, FileContent = "Error", Message = "ErrorMessage", IsError = true });

        var notificationServiceMock = new Mock<INotificationService>();

        var status = "Working";
        var errorStatus = default(string);
        var key = $"{userId}{organisationId}Subsidiary bulk upload progress";
        var errorKey = $"{userId}{organisationId}Subsidiary bulk upload errors";
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        notificationServiceMock.Verify(nft => nft.SetErrorStatus(errorKey, It.Is<List<UploadFileErrorModel>>(err => err[0].ErrorNumber > 0)), Times.Once);
    }

    [TestMethod]
    public async Task ShouldProcessFranchiseeWhenDataDoseNotExistForFranchisee()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();
        subsidiaries[0].franchisee_licensee_tenant = "Y";
        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        // Return a null OrganisationResponseModel to simulate the company not existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<LinkOrganisationModel>();
        subsidiaryService.Setup(ss => ss.CreateAndAddSubsidiaryAsync(It.IsAny<LinkOrganisationModel>()))
            .ReturnsAsync(HttpStatusCode.OK)
            .Callback<LinkOrganisationModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model => model.Error = null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
        .ReturnsAsync((OrganisationResponseModel?)null);

        var notificationServiceMock = new Mock<INotificationService>();
        var status = "Working";
        var errorStatus = default(string);
        var key = $"{userId}{organisationId}Subsidiary bulk upload progress";
        var errorKey = $"{userId}{organisationId}Subsidiary bulk upload errors";
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        var result = await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public async Task ShouldProcessMultipleFranchiseeWhenDataDoseNotExistForFranchisee()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(2)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(2).ToArray();
        subsidiaries[0].franchisee_licensee_tenant = "Y";
        subsidiaries[1].franchisee_licensee_tenant = "Y";
        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaryOrganisations[1].companiesHouseNumber = subsidiaries[1].companies_house_number;
        subsidiaryOrganisations[1].name = subsidiaries[1].organisation_name;

        // Return a null OrganisationResponseModel to simulate the company not existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[1].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var inserts = new List<LinkOrganisationModel>();
        subsidiaryService.Setup(ss => ss.CreateAndAddSubsidiaryAsync(It.IsAny<LinkOrganisationModel>()))
            .ReturnsAsync(HttpStatusCode.OK)
            .Callback<LinkOrganisationModel>(model => inserts.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model => model.Error = null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
        .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[1].organisation_name))
        .ReturnsAsync((OrganisationResponseModel?)null);

        var notificationServiceMock = new Mock<INotificationService>();
        var status = "Working";
        var errorStatus = default(string);
        var key = $"{userId}{organisationId}Subsidiary bulk upload progress";
        var errorKey = $"{userId}{organisationId}Subsidiary bulk upload errors";
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        var result = await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task ShouldProcessFranchiseeWhenDataExistForFranchisee()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();
        subsidiaries[0].franchisee_licensee_tenant = "Y";
        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        // Return an OrganisationResponseModel to simulate the company existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var updates = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => updates.Add(model));

        var notificationServiceMock = new Mock<INotificationService>();
        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
        .ReturnsAsync(true)
        .Callback<OrganisationModel>(model => model.Error = null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
         .ReturnsAsync(subsidiaryOrganisations[0]);

        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        var result = await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        updates.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task ShouldProcessSubsidiesWhenDataExistinRDP()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaries[0].franchisee_licensee_tenant = null;
        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        // Return an OrganisationResponseModel to simulate the company existing in RPD
        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var updates = new List<SubsidiaryAddModel>();
        subsidiaryService.Setup(ss => ss.AddSubsidiaryRelationshipAsync(It.IsAny<SubsidiaryAddModel>()))
            .Callback<SubsidiaryAddModel>(model => updates.Add(model));

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model => model.Error = null);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        var notificationServiceMock = new Mock<INotificationService>();

        var status = "Working";
        var errorStatus = default(string);
        var key = $"{userId}{organisationId}Subsidiary bulk upload progress";
        var errorKey = $"{userId}{organisationId}Subsidiary bulk upload errors";
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        var result = await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        updates.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task ShouldProcessErrorsWhenSubsidiesAPIDataNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1)
            .ToArray();

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaries[0].franchisee_licensee_tenant = null;

        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
            .ReturnsAsync((OrganisationResponseModel?)null);

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model => model.Error = new UploadFileErrorModel() { ErrorNumber = 101, FileContent = "Error", Message = "ErrorMessage", IsError = true });

        var notificationServiceMock = new Mock<INotificationService>();

        var status = "Working";
        var errorStatus = default(string);
        var key = $"{userId}{organisationId}Subsidiary bulk upload progress";
        var errorKey = $"{userId}{organisationId}Subsidiary bulk upload errors";
        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        notificationServiceMock.Verify(nft => nft.SetErrorStatus(errorKey, It.Is<List<UploadFileErrorModel>>(err => err[0].ErrorNumber > 0)), Times.Once);
    }

    [TestMethod]
    public async Task ShouldAddSubsidiaryWhenSubsidiaresAPIDataFoundWithNoErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        var subsidiaries = _fixture
            .Build<CompaniesHouseCompany>()
            .With(c => c.Errors, () => new())
            .CreateMany(1)
        .ToArray();

        var companiesHouseResponse = _fixture.Create<Company>();
        var config = _fixture.Create<TableStorageOptions>();
        var options = new Mock<IOptions<TableStorageOptions>>();
        options.Setup(o => o.Value).Returns(config);

        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();
        subsidiaries[0].franchisee_licensee_tenant = null;
        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;

        companiesHouseResponse.CompaniesHouseNumber = subsidiaryOrganisations[0].companiesHouseNumber;
        companiesHouseResponse.Name = subsidiaryOrganisations[0].name;
        companiesHouseResponse.Error = null;

        subsidiaryService.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(subsidiaries[0].companies_house_number))
            .ReturnsAsync((OrganisationResponseModel?)null);

        subsidiaryService.Setup(ss => ss.GetSubsidiaryRelationshipAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        subsidiaryService.Setup(ss => ss.GetCompanyByCompanyName(subsidiaries[0].organisation_name))
            .ReturnsAsync((OrganisationResponseModel?)null);

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();
        companiesHouseLookup.Setup(chl => chl.GetCompaniesHouseResponseAsync(subsidiaryOrganisations[0].companiesHouseNumber))
            .ReturnsAsync(companiesHouseResponse);

        var tableStorage = new Mock<ITableStorageProcessor>();
        tableStorage.Setup(ts => ts.GetByCompanyNumber(subsidiaryOrganisations[0].companiesHouseNumber, config.CompaniesHouseOfflineDataTableName))
            .ReturnsAsync((CompanyHouseTableEntity)null);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, tableStorage.Object, options.Object);

        var companiesHouseDataProvider = new Mock<ICompaniesHouseDataProvider>();
        companiesHouseDataProvider.Setup(ss => ss.SetCompaniesHouseData(It.IsAny<OrganisationModel>()))
            .ReturnsAsync(true)
            .Callback<OrganisationModel>(model =>
            {
                model.Error = null;
                model.CompaniesHouseCompanyName = subsidiaries[0].organisation_name;
                model.CompaniesHouseNumber = subsidiaries[0].companies_house_number;
            });

        var inserts = new List<LinkOrganisationModel>();
        subsidiaryService.Setup(ss => ss.CreateAndAddSubsidiaryAsync(It.IsAny<LinkOrganisationModel>()))
            .ReturnsAsync(HttpStatusCode.OK)
            .Callback<LinkOrganisationModel>(model => inserts.Add(model));

        var notificationServiceMock = new Mock<INotificationService>();

        var processor = new BulkSubsidiaryProcessor(subsidiaryService.Object, companiesHouseDataProvider.Object, NullLogger<BulkSubsidiaryProcessor>.Instance, notificationServiceMock.Object);

        var userRequestModel = new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };

        // Act
        var inserted = await processor.Process(subsidiaries, parent, parentOrganisation, userRequestModel);

        // Assert
        inserted.Should().Be(1);
    }
}
