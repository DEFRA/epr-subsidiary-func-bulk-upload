using System.Net;
using AutoFixture.AutoMoq;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CompaniesHouseLookupDirectServiceTests
{
    private const string CompaniesHouseEndpoint = "company";
    private const string CompaniesHouseNumber = "0123456X";
    private const string BaseAddress = "http://localhost";
    private const string ExpectedUrl = $"{BaseAddress}/{CompaniesHouseEndpoint}/{CompaniesHouseNumber}";

    private const string CompaniesHouseResponseJson =
        """
        {
          "accounts": {
            "accounting_reference_date": {
              "day": "31",
              "month": "10"
            },
            "last_accounts": { "type": "null" },
            "next_accounts": {
              "due_on": "2025-07-09",
              "overdue": false,
              "period_end_on": "2024-10-31",
              "period_start_on": "2023-10-09"
            },
            "next_due": "2025-07-09",
            "next_made_up_to": "2024-10-31",
            "overdue": false
          },
          "can_file": true,
          "company_name": "TEST LTD",
          "company_number": "0123456X",
          "company_status": "active",
          "confirmation_statement": {
            "next_due": "2024-10-22",
            "next_made_up_to": "2024-10-08",
            "overdue": false
          },
          "date_of_creation": "2023-10-09",
          "etag": "111111111111111",
          "has_charges": false,
          "has_insolvency_history": false,
          "jurisdiction": "wales",
          "links": {
            "persons_with_significant_control": "/company/0123456X/persons-with-significant-control",
            "self": "/company/0123456X",
            "filing_history": "/company/0123456X/filing-history",
            "officers": "/company/0123456X/officers"
          },
          "registered_office_address": {
            "address_line_1": "2 Test House",
            "address_line_2": "Twelve Roads",
            "country": "Wales",
            "locality": "Llanelli",
            "postal_code": "SA99 4YX"
          },
          "registered_office_is_in_dispute": false,
          "sic_codes": [ "56302" ],
          "type": "ltd",
          "undeliverable_registered_office_address": false,
          "has_super_secure_pscs": false
        }
        """;

    private const string CompaniesHouseErrorResponseJson =
        """
        {
          "errors": [
            {
              "error": "an-error",
              "type": "ch:service"
            }
          ]
        }
        """;

    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _httpMessageHandlerMock = new();
    }

    [TestMethod]
    public async Task Should_Return_Correct_CompaniesHouseLookupResponse()
    {
        // Arrange
        var expectedCompany = new Application.DTOs.Company
        {
            Name = "TEST LTD",
            CompaniesHouseNumber = "0123456X",
            BusinessAddress = new Address
            {
                Street = "2 Test House",
                Locality = "Twelve Roads",
                County = "Llanelli",
                Country = "Wales",
                Postcode = "SA99 4YX"
            }
        };

        _httpMessageHandlerMock.Protected()
             .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = new StringContent(CompaniesHouseResponseJson)
             }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupDirectService(httpClient);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());

        result.Should().NotBeNull();
        result.Name.Should().Be(expectedCompany.Name);
        result.CompaniesHouseNumber.Should().Be(expectedCompany.CompaniesHouseNumber);

        result.BusinessAddress.Should().NotBeNull();
        result.BusinessAddress.Country.Should().Be(expectedCompany.BusinessAddress.Country);
        result.BusinessAddress.County.Should().Be(expectedCompany.BusinessAddress.County);
        result.BusinessAddress.Town.Should().Be(expectedCompany.BusinessAddress.Town);
        result.BusinessAddress.Postcode.Should().Be(expectedCompany.BusinessAddress.Postcode);
        result.BusinessAddress.Street.Should().Be(expectedCompany.BusinessAddress.Street);
        result.BusinessAddress.Locality.Should().Be(expectedCompany.BusinessAddress.Locality);
    }

    [TestMethod]
    public async Task Should_Return_NotNull_When_ApiReturns_NoContent()
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

        var sut = new CompaniesHouseLookupDirectService(httpClient);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString() == ExpectedUrl), ItExpr.IsAny<CancellationToken>());
        result.Should().BeOfType<Company>();
        result.Error.Should().NotBeNull();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.Forbidden)]
    public async Task Should_return_Generic_Error_On_ApiReturns_Error(HttpStatusCode returnedStatusCode)
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = returnedStatusCode,
                Content = new StringContent(CompaniesHouseErrorResponseJson)
            }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupDirectService(httpClient);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        result.Should().BeOfType<Company>();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().BeSameAs("Unexpected error when retrieving data from Companies House. Try again later.");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NoContent)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Should_return_Error_On_ApiReturns_Error(HttpStatusCode returnedStatusCode)
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri != null && x.RequestUri.ToString() == ExpectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = returnedStatusCode,
                Content = new StringContent(CompaniesHouseErrorResponseJson)
            }).Verifiable();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(BaseAddress);

        var sut = new CompaniesHouseLookupDirectService(httpClient);

        // Act
        var result = await sut.GetCompaniesHouseResponseAsync(CompaniesHouseNumber);

        // Assert
        result.Should().BeOfType<Company>();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().BeSameAs("Information cannot be retrieved. Try again later.");
    }
}