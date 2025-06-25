using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class OrganisationServiceTests
{
    private const string BaseAddress = "http://localhost";
    private const string SyncStagingOrganisationDataUri = "api/organisations/sync-staging-organisation-data";

    private Fixture _fixture;
    private OrganisationService _sut;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private Mock<ILogger<OrganisationService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        _loggerMock = new Mock<ILogger<OrganisationService>>();

        _sut = new OrganisationService(_httpClient, _loggerMock.Object);
    }

    [TestMethod]
    public async Task SyncStagingToAccounts_ReturnsResponse()
    {
        // Arrange
        const string expectedUri = $"{BaseAddress}/{SyncStagingOrganisationDataUri}";
        var syncOrganisationStagingToAccountsModel = _fixture.Create<SyncOrganisationStagingToAccountsModel>();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = syncOrganisationStagingToAccountsModel.ToJsonContent()
            }).Verifiable();

        // Act
        var result = await _sut.SyncStagingToAccounts();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(syncOrganisationStagingToAccountsModel);
    }

    [TestMethod]
    public async Task GetCompanyByOrgId_ReturnsNull_When_NoSuccessResponse()
    {
        // Arrange
        const string expectedUri = $"{BaseAddress}/{SyncStagingOrganisationDataUri}";
        var apiResponse = _fixture.Create<ProblemDetails>();

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
        var result = await _sut.SyncStagingToAccounts();

        // Assert
        result.Should().BeNull();
    }
}