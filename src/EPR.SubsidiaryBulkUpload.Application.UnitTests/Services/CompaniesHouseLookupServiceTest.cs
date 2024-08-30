using System.Net;
using System.Text.Json;
using AutoFixture.AutoMoq;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CompaniesHouseLookupServiceTest
{
    private const string CompaniesHouseEndpoint = "CompaniesHouse/companies";
    private const string CompaniesHouseNumber = "tempCompaniesHouseNumber";
    private const string BaseAddress = "http://localhost";
    private const string ExpectedUrl = $"{BaseAddress}/{CompaniesHouseEndpoint}/{CompaniesHouseNumber}";
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<IOptions<ApiOptions>> _configOptionsMock;
    private Mock<ILogger<CompaniesHouseLookupService>> _loggerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _httpMessageHandlerMock = new();

        _configOptionsMock = new Mock<IOptions<ApiOptions>>();

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

        var sut = new CompaniesHouseLookupService(httpClient, _configOptionsMock.Object, _loggerMock.Object);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());

        result.Should().NotBeNull();
        result.Should().BeOfType<DTOs.Company>();
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

        var sut = new CompaniesHouseLookupService(httpClient, _configOptionsMock.Object, _loggerMock.Object);

        // Act
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber));

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());
        exception.Should().BeOfType<HttpRequestException>();
    }
}