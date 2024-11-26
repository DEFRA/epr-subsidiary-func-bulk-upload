using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CompaniesHouseDataProviderTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
    }

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromLocalStorageWithNameMatching()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var companyHouseEntity = fixture.Create<CompanyHouseTableEntity>();

        companyHouseEntity.CompanyName = organisationModel.Name;
        var companyHouseEntity1 = fixture.Create<CompanyHouseTableEntity>();
        var config = fixture.Create<TableStorageOptions>();
        var options = new Mock<IOptions<TableStorageOptions>>();
        options.Setup(o => o.Value).Returns(config);

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();
        var tableStorage = new Mock<ITableStorageProcessor>();

        tableStorage.Setup(ts => ts.GetByCompanyNumber(organisationModel.CompaniesHouseNumber, config.CompaniesHouseOfflineDataTableName))
            .ReturnsAsync(companyHouseEntity);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, tableStorage.Object, options.Object);

        // Act
        dataProvider.SetCompaniesHouseData(organisationModel);

        // Assert
        organisationModel.Address.Street.Should().Be(companyHouseEntity.RegAddressAddressLine1);
        organisationModel.Address.County.Should().Be(companyHouseEntity.RegAddressCounty);
        organisationModel.Address.Postcode.Should().Be(companyHouseEntity.RegAddressPostCode);
        organisationModel.Address.Town.Should().Be(companyHouseEntity.RegAddressPostTown);
        organisationModel.Address.Country.Should().Be(companyHouseEntity.RegAddressCountry);
        organisationModel.OrganisationType.Should().Be(OrganisationType.CompaniesHouseCompany);
    }

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromLocalStorageWithCompanyNameIsAMatch()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var companyHouseEntity = fixture.Create<CompanyHouseTableEntity>();

        companyHouseEntity.CompanyName = organisationModel.Name;
        var companyHouseEntity1 = fixture.Create<CompanyHouseTableEntity>();
        var config = fixture.Create<TableStorageOptions>();
        var options = new Mock<IOptions<TableStorageOptions>>();
        options.Setup(o => o.Value).Returns(config);

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();
        var tableStorage = new Mock<ITableStorageProcessor>();

        tableStorage.Setup(ts => ts.GetByCompanyNumber(organisationModel.CompaniesHouseNumber, config.CompaniesHouseOfflineDataTableName))
            .ReturnsAsync(companyHouseEntity);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, tableStorage.Object, options.Object);

        // Act
        dataProvider.SetCompaniesHouseData(organisationModel);

        // Assert
        organisationModel.Name.Should().NotBeNull();
        organisationModel.Name.Should().Be(companyHouseEntity.CompanyName);
    }

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromCompaniesHouseApi()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var companiesHouseResponse = fixture.Create<Company>();
        companiesHouseResponse.Error = null;
        var config = fixture.Create<TableStorageOptions>();
        var options = new Mock<IOptions<TableStorageOptions>>();
        options.Setup(o => o.Value).Returns(config);

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();

        var tableStorage = new Mock<ITableStorageProcessor>();

        tableStorage.Setup(ts => ts.GetByCompanyNumber(organisationModel.CompaniesHouseNumber, config.CompaniesHouseOfflineDataTableName))
            .ReturnsAsync((CompanyHouseTableEntity)null);

        companiesHouseLookup.Setup(chl => chl.GetCompaniesHouseResponseAsync(organisationModel.CompaniesHouseNumber))
            .ReturnsAsync(companiesHouseResponse);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, tableStorage.Object, options.Object);

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

    [TestMethod]
    public void ShouldSetCompaniesHouseDataFromCompaniesHouseApiWithErrors()
    {
        // Arrange
        var organisationModel = fixture.Create<OrganisationModel>();
        var companiesHouseResponse = fixture.Create<Company>();
        var config = fixture.Create<TableStorageOptions>();
        var options = new Mock<IOptions<TableStorageOptions>>();
        options.Setup(o => o.Value).Returns(config);

        var companiesHouseLookup = new Mock<ICompaniesHouseLookupService>();

        var tableStorage = new Mock<ITableStorageProcessor>();

        tableStorage.Setup(ts => ts.GetByCompanyNumber(organisationModel.CompaniesHouseNumber, config.CompaniesHouseOfflineDataTableName))
            .ReturnsAsync((CompanyHouseTableEntity)null);

        companiesHouseLookup.Setup(chl => chl.GetCompaniesHouseResponseAsync(organisationModel.CompaniesHouseNumber))
            .ReturnsAsync(companiesHouseResponse);

        var dataProvider = new CompaniesHouseDataProvider(companiesHouseLookup.Object, tableStorage.Object, options.Object);

        // Act
        dataProvider.SetCompaniesHouseData(organisationModel);

        // Assert
        organisationModel.Error.Should().NotBeNull();
        organisationModel.Error.FileContent.Should().NotBeNull();
    }
}
