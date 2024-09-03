using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class SubsidiaryServiceTests
{
    private const string BaseAddress = "http://localhost";
    private const string OrganisationByCompanyHouseNumberUri = "api/bulkuploadorganisations/";

    private SubsidiaryService _sut;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;

    public SubsidiaryServiceTests()
    {
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
        const string companyName = "OIOASDf";

        var companiesHouseNumber = "0123456X";
        const string organisationId = "525362";
        const string referenceNumber = "525362";
        const string buildingNumber = "1";
        const string street = "Main Street";
        const string postcode = "SW1A5 1AA";
        const string organisationType = "Regulators";
        const string countryName = "United Kingdom";

        var organisationResponseModels = new OrganisationResponseModel[]
        {
            new()
            {
                referenceNumber = referenceNumber,
                companiesHouseNumber = companiesHouseNumber,
                name = companyName,
                organisationType = organisationType,
                address = new()
                {
                    BuildingNumber = buildingNumber,
                    Street = street,
                    Postcode = postcode,
                    Country = countryName,
                }
            }
        };

        var expectedUri = $"{BaseAddress}/{OrganisationByCompanyHouseNumberUri}?companiesHouseNumber={companiesHouseNumber}";

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
        var result = await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OrganisationResponseModel>();
        result.name.Should().Be(companyName);
        result.companiesHouseNumber.Should().Be(companiesHouseNumber);
        result.referenceNumber.Should().Be(referenceNumber);
        result.organisationType.Should().Be(organisationType);
    }
}
