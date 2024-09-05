using System.Net;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Exceptions;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class SubsidiaryServiceTests
{
    private const string BaseAddress = "http://localhost";
    private const string OrganisationByCompanyHouseNumberUri = "api/bulkuploadorganisations/";

    private Fixture _fixture;

    private SubsidiaryService _sut;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        _sut = new SubsidiaryService(_httpClient, new NullLogger<SubsidiaryService>());
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
    public async Task GetCompanyByCompaniesHouseNumber_ThrowsProblemResponseException_When_NoSuccessResponse()
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
        Func<Task> act = async () => await _sut.GetCompanyByCompaniesHouseNumber(organisationResponseModel.companiesHouseNumber);

        // Assert
        await act.Should().ThrowAsync<ProblemResponseException>();
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
        Func<Task> act = async () => await _sut.GetCompanyByOrgId(company);

        // Assert
        await act.Should().ThrowAsync<ProblemResponseException>();
    }

    [TestMethod]
    public async Task CreateAndAddSubsidiaryAsync_Returns_Expected_Result()
    {
        // Arrange

        // Act

        // Act
        /* Task<string?> CreateAndAddSubsidiaryAsync(LinkOrganisationModel linkOrganisationModel); */

        // Assert
    }

    [TestMethod]
    public async Task AddSubsidiaryRelationshipAsync_Returns_Expected_Result()
    {
        // Arrange

        // Act
        /* Task<string?> AddSubsidiaryRelationshipAsync(SubsidiaryAddModel subsidiaryAddModel); */

        // Assert
    }

    [TestMethod]
    public async Task GetSubsidiaryRelationshipAsync_Returns_Expected_Result()
    {
        // Arrange

        // Act
        /* Task<bool> GetSubsidiaryRelationshipAsync(int parentOrganisationId, int subsidiaryOrganisationId); */

        // Assert
    }
}
