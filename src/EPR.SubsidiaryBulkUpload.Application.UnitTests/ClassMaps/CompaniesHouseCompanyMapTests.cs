using System.Text;
using EPR.SubsidiaryBulkUpload.Application.ClassMaps;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.ClassMaps;

[TestClass]
public class CompaniesHouseCompanyMapTests
{
    private const string _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type\n";
    private Mock<ISubsidiaryService> _mockSubsidiaySrevice;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockSubsidiaySrevice = new Mock<ISubsidiaryService>();
    }

    [TestMethod]
    [DataRow("123456")]
    [DataRow("")]
    public void ClassMap_Returns_Valid_Data(string companyHouseNumber)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = companyHouseNumber, parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Child", franchisee_licensee_tenant = "Y", joiner_date = "01/10/2024", reporting_type = "SELF", Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());
        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);
        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
        rows[0].Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void ClassMap_Returns_Valid_Header_Data()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
        rows[0].Errors.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("", "", "OrgA", "123456", "Child", "", "10/10/2024", "SELF", "The 'organisation id' column is missing.")]
    [DataRow("23123", "", "", "123456", "Child", "", "10/10/2024", "SELF", "The 'organisation name' column is missing.")]
    [DataRow("23123", "", "OrgA", "", "Child", "", "10/10/2024", "SELF", "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", " ", "Child", "", "10/10/2024", "SELF", "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "10/10/2024", "SELF", "The 'parent or child' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "10/10/2024", "SELF", "Your Companies House number must be 8 characters or fewer.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "10/10/2024", "SELF", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "A123 456 ", "Child", "", "10/10/2024", "SELF", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "SELF", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "Self", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "Group", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "self", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "10/10/2024", "group", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "", "SELF", "The 'joiner date' column is missing.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "GROUPA", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "Group1", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfi", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfum", "The 'reporting type' column only allowed 'GROUP' or 'SELF'")]
    public void ClassMap_Returns_Error(
        string organisationId,
        string subsidiaryId,
        string organisationName,
        string companiesHouseNumber,
        string parentChild,
        string franchiseeLicenseeTenant,
        string joinerDate,
        string reportingType,
        string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "GROUPA", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "Group1", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfi", "The 'reporting type' column only allowed 'GROUP' or 'SELF'.")]
    [DataRow("23123", "", "OrgA", "B123456", "Child", "", "10/10/2024", "salfum", "The 'reporting type' column only allowed 'GROUP' or 'SELF'")]
    public void ClassMap_Returns_Error_When_Wrong_ReportingType_Input(
       string organisationId,
       string subsidiaryId,
       string organisationName,
       string companiesHouseNumber,
       string parentChild,
       string franchiseeLicenseeTenant,
       string joinerDate,
       string reportingType,
       string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
    [DataRow("23123", "", "OrgA", "", "Child", "", "10/10/2024", "SELF", "The 'companies house number' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456", "Child", "NO", "10/10/2024", "SELF", "You can only enter 'Y' to the 'franchisee licensee tenant' column, or leave it blank.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "10/10/2024", "SELF", "The 'parent or child' column is missing.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "10/10/2024", "SELF", "Your Companies House number must be 8 characters or fewer.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "10/10/2024", "SELF", "Spaces in Companies House Number not allowed. Invalid Number.")]
    public void ClassMap_ValidationChecks_Returns_Error(
       string organisationId,
       string subsidiaryId,
       string organisationName,
       string companiesHouseNumber,
       string parentChild,
       string franchiseeLicenseeTenant,
       string joinerDate,
       string reportingType,
       string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, joiner_date = joinerDate, reporting_type = reportingType, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, CsvConfigurations.BulkUploadCsvConfiguration);

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
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

        var map = new CompaniesHouseCompanyMap(true, _mockSubsidiaySrevice.Object);
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