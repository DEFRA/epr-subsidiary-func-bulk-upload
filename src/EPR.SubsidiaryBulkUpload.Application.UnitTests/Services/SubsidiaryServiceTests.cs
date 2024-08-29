using System.Net;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Extensions;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class SubsidiaryServiceTests
{
    private SubsidiaryService _sut;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;

    public SubsidiaryServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com")
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
        const string buildingNumber = "1";
        const string street = "Main Street";
        const string referenceNumber = "SW1A51AA";
        const string organisationType = "Regulators";
        const string countryName = "United Kingdom";

        var company = new CompaniesHouseCompany
        {
            organisation_id = organisationId,
            companies_house_number = companiesHouseNumber,
            organisation_name = companyName,
            parent_child = "Parent"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = company.ToJsonContent();

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);

        // Assert
        result.Should().BeOfType<Company>();
        result.name.Should().Be(companyName);
        result.companiesHouseNumber.Should().Be(companiesHouseNumber);
        result.referenceNumber.Should().Be(referenceNumber);
        result.organisationType.Should().Be(organisationType);
    }
}
