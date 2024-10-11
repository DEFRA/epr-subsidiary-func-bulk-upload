using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.ClassMaps;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules don't yet support collection expressions")]
public class CompaniesHouseCompanyMapTests
{
    private const string _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";

    private CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        PrepareHeaderForMatch = args => args.Header.ToLower(),
        HasHeaderRecord = true,
        IgnoreBlankLines = false,
        MissingFieldFound = null,
        Delimiter = ",",
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null,
        HeaderValidated = args =>
        {
            if (args.Context?.Reader is CustomCsvReader csvReader)
            {
                csvReader.InvalidHeaderErrors = args.InvalidHeaders?.Select(x => x.Names[0]).ToList();
            }
        },
    };

    [TestMethod]
    public void ClassMap_Returns_Valid_Data()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, _csvConfiguration);

        csvReader.Context.RegisterClassMap<CompaniesHouseCompanyMap>();

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
    public void ClassMap_Returns_Valid_Data_Franchise()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Child", franchisee_licensee_tenant = "Y", Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, _csvConfiguration);

        csvReader.Context.RegisterClassMap<CompaniesHouseCompanyMap>();

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
    public void ClassMap_Returns_Valid_header_Data()
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = "Sub1", organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = new() }
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, _csvConfiguration);

        csvReader.Context.RegisterClassMap<CompaniesHouseCompanyMap>();

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
    [DataRow("", "", "OrgA", "123456", "Child", "", "organisation_id is required.")]
    [DataRow("23123", "", "", "123456", "Child", "", "organisation_name is required.")]
    [DataRow("23123", "", "OrgA", "", "Child", "", "companies_house_number is required.")]
    [DataRow("23123", "", "OrgA", " ", "Child", "", "companies_house_number is required.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "parent_or_child is required.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "Companies House Number Field length is invalid. 8 Characters allowed.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "A123 456 ", "Child", "", "Spaces in Companies House Number not allowed. Invalid Number.")]
    [DataRow("23123", "", "OrgA", "B123 456", "Child", "", "Spaces in Companies House Number not allowed. Invalid Number.")]
    public void ClassMap_Returns_Error(
        string organisationId,
        string subsidiaryId,
        string organisationName,
        string companiesHouseNumber,
        string parentChild,
        string franchiseeLicenseeTenant,
        string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, _csvConfiguration);

        csvReader.Context.RegisterClassMap<CompaniesHouseCompanyMap>();

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
    [DataRow("23123", "", "OrgA", "", "Child", "", "companies_house_number is required.")]
    [DataRow("23123", "", "OrgA", "123456", "Child", "NO", "franchisee_licensee_tenant can only be blank or Yes or Y.")]
    [DataRow("23123", "", "OrgA", "123456", "", "", "parent_or_child is required.")]
    [DataRow("23123", "", "OrgA", "123456789", "Child", "", "Companies House Number Field length is invalid. 8 Characters allowed.")]
    [DataRow("23123", "", "OrgA", " 123 456", "Child", "", "Spaces in Companies House Number not allowed. Invalid Number.")]
    public void ClassMap_validationchecks_Returns_Error(
       string organisationId,
       string subsidiaryId,
       string organisationName,
       string companiesHouseNumber,
       string parentChild,
       string franchiseeLicenseeTenant,
       string expectedErrorMessage)
    {
        // Arrange
        var dataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = organisationId,  subsidiary_id = subsidiaryId, organisation_name = organisationName, companies_house_number = companiesHouseNumber, parent_child = parentChild, franchisee_licensee_tenant = franchiseeLicenseeTenant, Errors = new() },
            };
        var rawSource = dataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        using var reader = new StreamReader(stream);
        using var csvReader = new CustomCsvReader(reader, _csvConfiguration);

        csvReader.Context.RegisterClassMap<CompaniesHouseCompanyMap>();

        // Act
        csvReader.Read();
        csvReader.ReadHeader();
        var rows = csvReader.GetRecords<CompaniesHouseCompany>().ToList();

        // Assert
        rows.Should().NotBeNullOrEmpty();
        rows[0].Errors.Should().HaveCount(1);
        rows[0].Errors[0].Message.Should().Contain(expectedErrorMessage);
    }
}