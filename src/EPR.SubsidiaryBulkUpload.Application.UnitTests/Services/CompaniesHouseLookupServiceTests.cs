using System.Net;
using System.Text.Json;
using AutoFixture.AutoMoq;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CompaniesHouseLookupServiceTests
{
    private const string CompaniesHouseEndpoint = "CompaniesHouse/companies";
    private const string CompaniesHouseNumber = "0123456X";
    private const string BaseAddress = "http://localhost";
    private const string ExpectedUrl = $"{BaseAddress}/{CompaniesHouseEndpoint}/{CompaniesHouseNumber}";
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<ILogger<CompaniesHouseLookupService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _httpMessageHandlerMock = new();

        _loggerMock = new Mock<ILogger<CompaniesHouseLookupService>>();
    }

    [TestMethod]
    public async Task Should_Return_Correct_CompaniesHouseLookupResponse()
    {
        // Arrange
        var apiResponse = _fixture.Create<CompaniesHouseResponse>();

        _httpMessageHandlerMock.Protected()
             .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = new StringContent(JsonSerializer.Serialize(apiResponse))
             }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());

        result.Should().NotBeNull();
        result.Name.Should().Be(apiResponse.Organisation.Name);
        result.CompaniesHouseNumber.Should().Be(apiResponse.Organisation.RegistrationNumber);
        result.AccountCreatedOn.Should().Be(apiResponse.AccountCreatedOn);

        result.BusinessAddress.Should().NotBeNull();
        result.BusinessAddress.Country.Should().Be(apiResponse.Organisation.RegisteredOffice?.Country?.Name);
        result.BusinessAddress.County.Should().Be(apiResponse.Organisation.RegisteredOffice.County);
        result.BusinessAddress.Town.Should().Be(apiResponse.Organisation.RegisteredOffice.Town);
        result.BusinessAddress.Postcode.Should().Be(apiResponse.Organisation.RegisteredOffice.Postcode);
        result.BusinessAddress.Street.Should().Be(apiResponse.Organisation.RegisteredOffice.Street);
        result.BusinessAddress.Locality.Should().Be(apiResponse.Organisation.RegisteredOffice.Locality);
    }

    [TestMethod]
    public async Task Should_Return_Null_When_ApiReturns_NoContent()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
             .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage
             {
                 StatusCode = HttpStatusCode.NoContent
             }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Should_Return_Null_When_Response_StatusCode_NotFound()
    {
        // Arrange
        var errorResponse = new CompaniesHouseErrorResponse
        {
            InnerException = new InnerExceptionResponse
            {
                Code = ((int)HttpStatusCode.NotFound).ToString()
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Should_Throw_Exception_On_ApiReturns_Error(HttpStatusCode returnedStatusCode)
    {
        // Arrange
        var errorResponse = new CompaniesHouseErrorResponse
        {
            InnerException = new InnerExceptionResponse
            {
                Code = ((int)returnedStatusCode).ToString()
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = returnedStatusCode,
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupService(httpClient, _loggerMock.Object);

        // Act
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber));

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());
        exception.Should().BeOfType<HttpRequestException>();
    }
}