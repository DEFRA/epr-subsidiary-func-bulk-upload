using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Service;

[TestClass]
public class CompaniesHouseDataProviderTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitiaize()
    {
        fixture = new();
    }

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromLocalStorage()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var lookupResponse = fixture.Create<OrganisationModel>();

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        subsidiaryService.Setup(ss => ss.GetCompanyByOrgIdFromTableStorage(organisationModel.CompaniesHouseNumber))
            .ReturnsAsync(lookupResponse);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, subsidiaryService.Object);

        // Act
        dataProvider.SetCompaniesHouseData(organisationModel);

        // Assert
        organisationModel.Address.Should().BeEquivalentTo(lookupResponse.Address);
        organisationModel.OrganisationType.Should().Be(OrganisationType.NotSet);
    }

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromCompaniesHouseApi()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var companiesHouseResponse = fixture.Create<Company>();

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();
        var subsidiaryService = new Mock<ISubsidiaryService>();

        subsidiaryService.Setup(ss => ss.GetCompanyByOrgIdFromTableStorage(organisationModel.CompaniesHouseNumber))
            .ReturnsAsync((OrganisationModel)null);

        companiesHouseLookup.Setup(chl => chl.GetCompaniesHouseResponseAsync(organisationModel.CompaniesHouseNumber))
            .ReturnsAsync(companiesHouseResponse);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, subsidiaryService.Object);

        // Act
        dataProvider.SetCompaniesHouseData(organisationModel);

        // Assert
        organisationModel.Name.Should().Be(companiesHouseResponse.Name);
        organisationModel.OrganisationType.Should().Be(OrganisationType.CompaniesHouseCompany);
        organisationModel.Address.BuildingNumber.Should().Be(companiesHouseResponse.BusinessAddress.BuildingNumber);
        organisationModel.Address.Street.Should().Be(companiesHouseResponse.BusinessAddress.Street);
        organisationModel.Address.Country.Should().Be(companiesHouseResponse.BusinessAddress.Country);
        organisationModel.Address.Locality.Should().Be(companiesHouseResponse.BusinessAddress.Locality);
        organisationModel.Address.Postcode.Should().Be(companiesHouseResponse.BusinessAddress.Postcode);
    }
}
