using System.Text;
using EPR.SubsidiaryBulkUpload.Application.ClassMaps;
using EPR.SubsidiaryBulkUpload.Application.Constants;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.FeatureManagement;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.ClassMaps;

[TestClass]
public class CompaniesHouseCompanyMapTests
{
    private const string _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code\n";
    private Mock<IFeatureManager> _mockFeatureManager;
    private Mock<ISubsidiaryService> _mockSubsidiaySrevice;
    private Fixture _fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _mockFeatureManager = new Mock<IFeatureManager>();
        _mockSubsidiaySrevice = new Mock<ISubsidiaryService>();
        _mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerColumns))
            .ReturnsAsync(true);
    }

    [TestMethod]
    [DataRow("123456")]
    [DataRow("")]
    public void ClassMap_Returns_Valid_Data(string companyHouseNumber)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = companyHouseNumber, parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = NationCode.EN, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows.Count.Should().Be(1);
        rows[0].organisation_id.Should().Be("23123");
        rows[0].subsidiary_id.Should().Be("Sub1");
        rows[0].organisation_name.Should().Be("OrgA");
        rows[0].companies_house_number.Should().Be(companyHouseNumber);
        rows[0].parent_child.Should().Be("Parent");
        rows[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        rows[0].Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void ClassMap_Returns_Valid_Data_Franchise()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Child", franchisee_licensee_tenant = "Y", joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = NationCode.EN, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());
        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);
        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows.Count.Should().Be(1);
        rows[0].organisation_id.Should().Be("23123");
        rows[0].subsidiary_id.Should().Be("Sub1");
        rows[0].organisation_name.Should().Be("OrgA");
        rows[0].companies_house_number.Should().Be("123456");
        rows[0].parent_child.Should().Be("Child");
        rows[0].franchisee_licensee_tenant.Should().Be("Y");
        rows[0].nation_code.Should().Be(NationCode.EN);
        rows[0].Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void ClassMap_Returns_Valid_Header_Data()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = NationCode.EN, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows.Count.Should().Be(1);
        rows[0].organisation_id.Should().Be("23123");
        rows[0].subsidiary_id.Should().Be("Sub1");
        rows[0].organisation_name.Should().Be("OrgA");
        rows[0].companies_house_number.Should().Be("123456");
        rows[0].parent_child.Should().Be("Parent");
        rows[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        rows[0].nation_code.Should().Be(NationCode.EN);
        rows[0].Errors.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("", "", "OrgA", "123456", "Child", "", "10/10/2024", "SELF", NationCode.EN, "The 'organisation id' column is missing.")]
    [DataRow("23123", "", "", "123456", "Child", "", "10/10/2024", "SELF", NationCode.EN, "The 'organisation name' column is missing.")]
    [DataRow("23123", "", "OrgA", "", "Child", "", "10/10/2024", "SELF", NationCode.EN, "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", " ", "Child", "", "10/10/2024", "SELF", NationCode.EN, "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "10/10/2024", "SELF", NationCode.EN, "The 'parent or child' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Your Companies House number must be 8 characters or fewer.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "A123 456 ", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "Self", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "Group", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "self", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "group", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "", "SELF", NationCode.EN, "The 'joiner date' column is missing.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "GROUPA", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "Group1", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfi", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfum", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'")]
    public void ClassMap_Returns_Error(
        string organisationId,
        string subsidiaryId,
        string organisationName,
        string companiesHouseNumber,
        string parentChild,
        string franchiseeLicenseeTenant,
        string joinerDate,
        string reportingType,
        NationCode nationCode,
        string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, nation_code = nationCode, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaryOrganisations[0].reportingType = "Self";
        subsidiaryOrganisations[0].joinerDate = null;
        subsidiaryOrganisations[0].OrganisationRelationship.JoinerDate = DateTime.Now.AddDays(-10);
        subsidiaryOrganisations[0].OrganisationRelationship.ReportingTypeId = 1;

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());
        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(parentOrganisation);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "", "SELF", NationCode.EN, "The 'joiner date' column is missing.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "GROUPA", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "Group1", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfi", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfum", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'")]
    public void ClassMap_JoinerDate_Returns_Error(
        string organisationId,
        string subsidiaryId,
        string organisationName,
        string companiesHouseNumber,
        string parentChild,
        string franchiseeLicenseeTenant,
        string joinerDate,
        string reportingType,
        NationCode nationCode,
        string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, nation_code = nationCode, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaryOrganisations[0].reportingType = "Self";
        subsidiaryOrganisations[0].joinerDate = null;
        subsidiaryOrganisations[0].OrganisationRelationship.JoinerDate = DateTime.Now.AddDays(-10);
        subsidiaryOrganisations[0].OrganisationRelationship.ReportingTypeId = 1;

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        subsidiaryOrganisations[0].OrganisationRelationship.FirstOrganisationId = parentOrganisation.id;

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(parentOrganisation);

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());
        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "GROUPA", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "Group1", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfi", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfum", NationCode.EN, "The 'reporting type' column only allowed 'GROUP' or 'SELF'")]
    public void ClassMap_Returns_Error_When_Wrong_ReportingType_Input(
       string organisationId,
       string subsidiaryId,
       string organisationName,
       string companiesHouseNumber,
       string parentChild,
       string franchiseeLicenseeTenant,
       string joinerDate,
       string reportingType,
       NationCode nationCode,
       string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, nation_code = nationCode, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    [DataRow("23123", "", "OrgA", "", "Child", "", "10/10/2024", "SELF", NationCode.EN, "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456", "Child", "NO", "10/10/2024", "SELF", NationCode.EN, "You can only enter 'Y' to the 'franchisee licensee tenant' column, or leave it blank.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "10/10/2024", "SELF", NationCode.EN, "The 'parent or child' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Your Companies House number must be 8 characters or fewer.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "10/10/2024", "SELF", NationCode.EN, "Spaces in Companies House Number not allowed. Invalid Number.")]
    public void ClassMap_ValidationChecks_Returns_Error(
       string organisationId,
       string subsidiaryId,
       string organisationName,
       string companiesHouseNumber,
       string parentChild,
       string franchiseeLicenseeTenant,
       string joinerDate,
       string reportingType,
       NationCode nationCode,
       string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, nation_code = nationCode, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    public void ClassMap_ValidationChecks_Returns_NationCodeAbsentError()
    {
        // Arrange
        const string expectedErrorMessage = "The file must contain a column for nation code.";
        var dataModel = new List<CompaniesHouseCompany>
        {
            new()
            {
                organisation_id = "23123",
                subsidiary_id = string.Empty,
                organisation_name = "OrgA",
                companies_house_number = "123456",
                parent_child = "Child",
                franchisee_licensee_tenant = string.Empty,
                joiner_date = "10/10/2024",
                reporting_type = "SELF",
                nation_code = null,
                Errors = new()
            },
        };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    public void ClassMap_ValidationChecks_Returns_NationCodeInvalidError()
    {
        // Arrange
        const string expectedErrorMessage = "The file must contain a column for nation code.";
        var dataModel = new List<CompaniesHouseCompany>
        {
            new()
            {
                organisation_id = "23123",
                subsidiary_id = string.Empty,
                organisation_name = "OrgA",
                companies_house_number = "123456",
                parent_child = "Child",
                franchisee_licensee_tenant = string.Empty,
                joiner_date = "10/10/2024",
                reporting_type = "SELF",
                nation_code = NationCode.EN,
                Errors = new()
            },
        };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},XX\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [TestMethod]
    public void ClassMap_Returns_Valid_Header_Data_And_Ignores_Empty_Rows()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = NationCode.EN, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [
            _csvHeader,
            rawSource.First(),
            "     \r\n",
            "\r\n",
            "\r\n"
            ];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object, true);
        csvReader.Context.RegisterClassMap(map);

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows.Count.Should().Be(2);

        rows[0].organisation_id.Should().Be(dataModel[0].organisation_id);
        rows[0].subsidiary_id.Should().Be(dataModel[0].subsidiary_id);
        rows[0].organisation_name.Should().Be(dataModel[0].organisation_name);
        rows[0].companies_house_number.Should().Be(dataModel[0].companies_house_number);
        rows[0].parent_child.Should().Be(dataModel[0].parent_child);
        rows[0].franchisee_licensee_tenant.Should().Be(dataModel[0].franchisee_licensee_tenant);
        rows[0].Errors.Should().BeEmpty();

        rows[1].organisation_id.Should().Be(string.Empty);
        rows[1].subsidiary_id.Should().Be(string.Empty);
        rows[1].organisation_name.Should().Be(string.Empty);
        rows[1].companies_house_number.Should().Be(string.Empty);
        rows[1].parent_child.Should().Be(string.Empty);
        rows[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        rows[1].Errors.Should().BeEmpty();
    }
}