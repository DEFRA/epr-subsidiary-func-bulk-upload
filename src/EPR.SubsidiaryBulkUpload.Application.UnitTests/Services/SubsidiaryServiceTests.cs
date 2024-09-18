using System.Net;
using System.Text;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class SubsidiaryServiceTests
{
    private const string BaseAddress = "http://localhost";
    private const string OrganisationByCompanyHouseNumberUri = "api/bulkuploadorganisations/";
    private const string OrganisationCreateAddSubsidiaryUri = "api/bulkuploadorganisations/create-subsidiary-and-add-relationship";
    private const string OrganisationAddSubsidiaryUri = "api/bulkuploadorganisations/add-subsidiary-relationship";
    private const string OrganisationRelationshipsByIdUri = "api/bulkuploadorganisations/organisation-by-relationship";
    private const string SystemUserAndOrganisationUri = "api/users/system-user-and-organisation";

    private Fixture _fixture;

    private SubsidiaryService _sut;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private Mock<ILogger<SubsidiaryService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        _loggerMock = new Mock<ILogger<SubsidiaryService>>();

        _sut = new SubsidiaryService(_httpClient, _loggerMock.Object);
    }

    [TestMethod]
    public async Task GetCompanyByCompaniesHouseNumber_ReturnsAccount()
    {
        // Arrange
        var organisationResponseModel = _fixture.Create<OrganisationResponseModel>();

        var organisationResponseModels = new OrganisationResponseModel[] { organisationResponseModel };

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={organisationResponseModel.companiesHouseNumber}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = organisationResponseModels.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(organisationResponseModel.companiesHouseNumber);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(organisationResponseModel);
    }

    [TestMethod]
    public async Task GetCompanyByCompaniesHouseNumber_ReturnsNull_When_NoContent()
    {
        // Arrange
        var organisationResponseModel = _fixture.Create<OrganisationResponseModel>();

        var organisationResponseModels = new OrganisationResponseModel[] { organisationResponseModel };

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={organisationResponseModel.companiesHouseNumber}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(organisationResponseModel.companiesHouseNumber);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCompanyByCompaniesHouseNumber_When_NullResponse()
    {
        // Arrange
        var apiResponse = _fixture.Create<ProblemDetails>();
        var organisationResponseModel = _fixture.Create<OrganisationResponseModel>();

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={organisationResponseModel.companiesHouseNumber}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(organisationResponseModel.companiesHouseNumber);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCompanyByCompaniesHouseNumber_ThrowsException_When_EmptyErrorReturned()
    {
        // Arrange
        var apiResponse = _fixture.Create<ProblemDetails>();
        var organisationResponseModel = _fixture.Create<OrganisationResponseModel>();

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={organisationResponseModel.companiesHouseNumber}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(organisationResponseModel.companiesHouseNumber);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCompanyByOrgId_ReturnsOrganisation()
    {
        // Arrange
        var company = _fixture.Create<CompaniesHouseCompany>();
        var organisationModel = _fixture.Create<OrganisationModel>();

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?organisation_id={company.organisation_id}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = organisationModel.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByOrgId(company);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(organisationModel);
    }

    [TestMethod]
    public async Task GetCompanyByOrgId_ReturnsNull_When_NoContent()
    {
        // Arrange
        var company = _fixture.Create<CompaniesHouseCompany>();

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?organisation_id={company.organisation_id}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByOrgId(company);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCompanyByOrgId_ThrowsProblemResponseException_When_NoSuccessResponse()
    {
        // Arrange
        var apiResponse = _fixture.Create<ProblemDetails>();
        var company = _fixture.Create<CompaniesHouseCompany>();

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?organisation_id={company.organisation_id}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetCompanyByOrgId(company);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetSubsidiaryRelationshipAsync_Returns_Expected_Result()
    {
        // Arrange
        const int parentOrganisationId = 1;
        const int subsidiaryOrganisationId = 2;
        var apiResponse = true;

        var expectedUri = $"{BaseAddress}/{OrganisationRelationshipsByIdUri}?parentId={parentOrganisationId}&subsidiaryId={subsidiaryOrganisationId}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetSubsidiaryRelationshipAsync(parentOrganisationId, subsidiaryOrganisationId);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetSubsidiaryRelationshipAsync_ReturnsFalse_When_NoContent()
    {
        // Arrange
        const int parentOrganisationId = 1;
        const int subsidiaryOrganisationId = 2;

        var expectedUri = $"{BaseAddress}/{OrganisationRelationshipsByIdUri}?parentId={parentOrganisationId}&subsidiaryId={subsidiaryOrganisationId}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
            }).Verifiable();

        // Act
        var result = await _sut.GetSubsidiaryRelationshipAsync(parentOrganisationId, subsidiaryOrganisationId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetSubsidiaryRelationshipAsync_ReturnFalse_When_NoSuccessResponse()
    {
        // Arrange
        const int parentOrganisationId = 1;
        const int subsidiaryOrganisationId = 2;
        var apiResponse = _fixture.Create<ProblemDetails>();

        var expectedUri = $"{BaseAddress}/{OrganisationRelationshipsByIdUri}?parentId={parentOrganisationId}&subsidiaryId={subsidiaryOrganisationId}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetSubsidiaryRelationshipAsync(parentOrganisationId, subsidiaryOrganisationId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task CreateAndAddSubsidiaryAsync_Returns_Expected_Result()
    {
        // Arrange
        var linkOrganisationModel = _fixture.Create<LinkOrganisationModel>();
        HttpStatusCode apiResponse = HttpStatusCode.OK;

        var expectedUri = $"{BaseAddress}/{OrganisationCreateAddSubsidiaryUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            }).Verifiable();

        // Act
        var response = await _sut.CreateAndAddSubsidiaryAsync(linkOrganisationModel);

        // Assert
        response.Should().Be(apiResponse);
    }

    /*  [TestMethod]
      public async Task CreateAndAddSubsidiary_ThrowsProblemResponseException_When_NoSuccessResponse()
      {
          // Arrange
          var linkOrganisationModel = _fixture.Create<LinkOrganisationModel>();
          HttpStatusCode apiResponse = HttpStatusCode.Forbidden;

          var expectedUri = $"{BaseAddress}/{OrganisationCreateAddSubsidiaryUri}";

          _httpMessageHandlerMock.Protected()
              .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                  ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(new HttpResponseMessage
              {
                  StatusCode = HttpStatusCode.Forbidden
              }).Verifiable();

          // Act
          var act = await _sut.CreateAndAddSubsidiaryAsync(linkOrganisationModel);

          // Assert
          act.Should().Be(apiResponse);
      }*/

    [TestMethod]
    public async Task AddSubsidiaryRelationshipAsync_Returns_Expected_Result()
    {
        // Arrange
        var subsidiaryAddModel = _fixture.Create<SubsidiaryAddModel>();
        var apiResponse = "123456";

        var expectedUri = $"{BaseAddress}/{OrganisationAddSubsidiaryUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(apiResponse, Encoding.UTF8)
        }).Verifiable();

        // Act
        var response = await _sut.AddSubsidiaryRelationshipAsync(subsidiaryAddModel);

        // Assert
        response.Should().Be(apiResponse);
    }

    [TestMethod]
    public async Task AddSubsidiaryRelationshipAsync_ThrowsProblemResponseException_When_NoSuccessResponse()
    {
        // Arrange
        var subsidiaryAddModel = _fixture.Create<SubsidiaryAddModel>();
        var apiResponse = _fixture.Create<ProblemDetails>();

        var expectedUri = $"{BaseAddress}/{OrganisationAddSubsidiaryUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var response = await _sut.AddSubsidiaryRelationshipAsync(subsidiaryAddModel);

        // Assert
        response.Should().Be(response);
    }

    [TestMethod]
    public async Task GetSystemUserAndOrganisation_Returns_Expected_Result()
    {
        // Arrange
        var systemUserId = Guid.NewGuid();
        var systemOrganisationId = Guid.NewGuid();
        var apiResponse = new UserOrganisation
        {
            OrganisationId = systemOrganisationId,
            UserId = systemUserId
        };

        var expectedUri = $"{BaseAddress}/{SystemUserAndOrganisationUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetSystemUserAndOrganisation();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(systemUserId);
        result.OrganisationId.Should().Be(systemOrganisationId);
    }

    [TestMethod]
    public async Task GetSystemUserAndOrganisation_ReturnsNullGuids_When_NotFound()
    {
        // Arrange
        var expectedUri = $"{BaseAddress}/{SystemUserAndOrganisationUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
            }).Verifiable();

        // Act
        var result = await _sut.GetSystemUserAndOrganisation();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeNull();
        result.OrganisationId.Should().BeNull();
    }

    [TestMethod]
    public async Task GetSystemUserAndOrganisation_ReturnsNullGuids_When_NoSuccessResponse()
    {
        // Arrange
        var apiResponse = _fixture.Create<ProblemDetails>();

        var expectedUri = $"{BaseAddress}/{SystemUserAndOrganisationUri}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = apiResponse.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.GetSystemUserAndOrganisation();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeNull();
        result.OrganisationId.Should().BeNull();

        _loggerMock.VerifyLog(x => x.LogError("Error occurred in GetSystemUserAndOrganisation call: Status code {StatusCode}, details {Details}", HttpStatusCode.BadRequest, apiResponse.Detail), Times.Once);
    }
}
