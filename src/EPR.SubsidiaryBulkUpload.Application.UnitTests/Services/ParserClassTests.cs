using System.Text;
using EPR.SubsidiaryBulkUpload.Application.Constants;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Pipelines.Sockets.Unofficial.Arenas;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class ParserClassTests
{
    private const string _csvHeaderWFFALSE = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
    private const string _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code\n";
    private const string _csvHeaderWithoutJoinerColumns = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,nation_code\n";
    private const string _csvHeaderWithoutNationCode = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type\n";
    private const string _csvHeaderWithMissingSubsidiaryId = "organisation_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code\n";
    private const string _csvHeaderWithNullValues = "";
    private const string _badHeader = "organisation,subsidiary,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code\n";
    private const string _badHeaderWithExtras = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code,invalid_item\n";

    private Fixture _fixture;
    private List<CompaniesHouseCompany> _listDataModel = null;
    private Mock<ILogger<ParserClass>> _loggerMock;
    private ParserClass _sut;
    private Mock<IFeatureManager> _mockFeatureManager;
    private Mock<ISubsidiaryService> _mockSubsidiaySrevice;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();

        _loggerMock = new Mock<ILogger<ParserClass>>();
        _mockFeatureManager = new Mock<IFeatureManager>();
        _mockSubsidiaySrevice = new Mock<ISubsidiaryService>();
        _mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerColumns))
            .ReturnsAsync(true);

        _mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableSubsidiaryNationColumn))
            .ReturnsAsync(true);

        _listDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = "EN", Errors = new() },
                new() { organisation_id = "23123", subsidiary_id = "Sub1", organisation_name = "OrgB", companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = "EN", Errors = new() }
            };

        _sut = new ParserClass(_loggerMock.Object, _mockFeatureManager.Object, _mockSubsidiaySrevice.Object);
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnEmptyData()
    {
        string[] all = [_csvHeader];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().BeEmpty();
    }

    [TestMethod]
    public void ParseClass_InvalidCsvHeader_ReturnsError()
    {
        var rawSource = _listDataModel.Take(1).Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_badHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);

        var errorRow = returnValue.CompaniesHouseCompany[0];

        errorRow.Errors.Should().NotBeEmpty();
        errorRow.Errors[0].FileContent.Should().Be("organisation,subsidiary,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code");
        errorRow.Errors[0].Message.Should().Contain("The headers are missing.");
    }

    [TestMethod]
    public void ParseClass_Missing_Column_ReturnsError()
    {
        var rawSource = _listDataModel.Take(1).Select(s => $"{s.organisation_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeaderWithMissingSubsidiaryId, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);

        var errorRow = returnValue.CompaniesHouseCompany[0];

        errorRow.Errors[0].Should().NotBeNull();
        errorRow.Errors[0].FileContent.Should().Be("organisation_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code");
        errorRow.Errors[0].Message.Should().Contain("The headers are missing.");
    }

    [TestMethod]
    public void ParseClass_ExtraColumn_IsError()
    {
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");

        string[] all = [_badHeaderWithExtras, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);

        var errorRow = returnValue.CompaniesHouseCompany[0];

        errorRow.Errors[0].Should().NotBeNull();
        errorRow.Errors[0].FileContent.Should().Be("organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,joiner_date,reporting_type,nation_code,invalid_item");
        errorRow.Errors[0].Message.Should().Contain("The file has additional column headers: The file has too many column headers. Remove these and try again.");

        var parsedResult = returnValue.CompaniesHouseCompany;
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData()
    {
        // Arrange
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].nation_code.Should().Be("EN");
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].nation_code.Should().Be("EN");
        parsedResult[1].Errors.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData_With_Flag()
    {
        // Arrange
        _mockFeatureManager.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);

        // var enableSubsidiaryJoinerColumns = _mockFeatureManager.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerColumns).GetAwaiter().GetResult();
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].nation_code.Should().Be("EN");
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].nation_code.Should().Be("EN");
        parsedResult[1].Errors.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData_With_Flag_False()
    {
        // Arrange
        _mockFeatureManager.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);

        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeaderWFFALSE, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].nation_code.Should().BeNull();
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].nation_code.Should().BeNull();
        parsedResult[1].Errors.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData_With_Flag_False_With_Extra_Column()
    {
        // Arrange
        _mockFeatureManager.Setup(x => x.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);
        var parsedResult = returnValue.CompaniesHouseCompany;
        parsedResult[0].Errors.Should().NotBeEmpty();
        parsedResult[0].Errors[0].Message.Should().Contain("The file has additional column headers: The file has too many column headers. Remove these and try again");
    }

    [TestMethod]
    public void ParseClass_InvalidData_ReturnsErrorMessage()
    {
        // Arrange
        var badDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = "EN", Errors = new() },
                new() { organisation_id = "23123", subsidiary_id = "Sub1", organisation_name = string.Empty, companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = "License123", joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = "EN", Errors = new() }
            };

        var rawSource = badDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1).ToArray();
        var subsidiaryOrganisations = _fixture.CreateMany<OrganisationResponseModel>(1).ToArray();

        subsidiaryOrganisations[0].companiesHouseNumber = subsidiaries[0].companies_house_number;
        subsidiaryOrganisations[0].name = subsidiaries[0].organisation_name;
        subsidiaryOrganisations[0].reportingType = "Self";
        subsidiaryOrganisations[0].joinerDate = null;
        subsidiaryOrganisations[0].nation = "EN";
        subsidiaryOrganisations[0].OrganisationRelationship.JoinerDate = DateTime.Now.AddDays(-10);
        subsidiaryOrganisations[0].OrganisationRelationship.ReportingTypeId = 1;

        var parent = _fixture.Create<CompaniesHouseCompany>();
        var parentOrganisation = _fixture.Create<OrganisationResponseModel>();

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(subsidiaryOrganisations[0]);

        _mockSubsidiaySrevice.Setup(ss => ss.GetCompanyByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(parentOrganisation);

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be(string.Empty);
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be("License123");

        parsedResult[1].Errors.Should().NotBeEmpty();
        parsedResult[1].Errors[0].Message.Should().Contain("The 'organisation name' column is missing.");
    }

    [TestMethod]
    public void ParseClass_Exception_ReturnsMessage()
    {
        var returnValue = _sut.ParseWithHelper(null, CsvConfigurations.BulkUploadCsvConfiguration);

        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeFalse();
        returnValue.ResponseClass.Messages.Should().NotBeNull();
    }

    [TestMethod]
    public void ParseClass_Exception_EmptyFile_ReturnsMessage()
    {
        var rawSource = Array.Empty<string>();
        string[] all = [_csvHeaderWithNullValues, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);

        var errorRow = returnValue.CompaniesHouseCompany[0];

        errorRow.Errors.Should().NotBeEmpty();
        errorRow.Errors[0].FileContent.Should().BeEmpty();
        errorRow.Errors[0].Message.Should().Contain("The file is empty. It does not contain headers or data rows.");
    }

    [TestMethod]
    public void ParseClass_InvalidCsvFile_StreamErrorReturnsErrorAndLogs()
    {
        // Arrange
        using var stream = new ErrorThrowingStream([0x00]);

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(1);

        returnValue.CompaniesHouseCompany[0].Errors.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ParseClass_InvalidReportingData_ReturnsErrorMessage()
    {
        // Arrange
        var badDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, joiner_date = "01/10/2024", reporting_type = "SELF", nation_code = "EN", Errors = new() },
                new() { organisation_id = "23123", subsidiary_id = "Sub1", organisation_name = string.Empty, companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = "License123", joiner_date = "01/10/2024", reporting_type = string.Empty, nation_code = "EN", Errors = new() }
            };

        var rawSource = badDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type},{s.nation_code}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be(string.Empty);
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be("License123");

        parsedResult[1].Errors.Should().NotBeEmpty();
        parsedResult[1].Errors[0].Message.Should().Contain("The 'organisation name' column is missing.");
        parsedResult[1].Errors[1].Message.Should().Contain("You can only enter 'Y' to the 'franchisee licensee tenant' column, or leave it blank.");
        parsedResult[1].Errors[2].Message.Should().Contain("The 'reporting type' column is missing.");
    }

    [TestMethod]
    public void ParseClass_ValidCsvFileWithNationFlagFalse_ReturnsCorrectData()
    {
        // Arrange
        _mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableSubsidiaryNationColumn))
            .ReturnsAsync(false);

        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.joiner_date},{s.reporting_type}\n");
        string[] all = [_csvHeaderWithoutNationCode, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].Errors.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void ParseClass_ValidCsvFileWithIncludeSubsidiaryJoinerColumnsFalseNationFlagTrue_ReturnsCorrectData()
    {
        // Arrange
        _mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableSubsidiaryJoinerColumns))
            .ReturnsAsync(false);

        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant},{s.nation_code}\n");
        string[] all = [_csvHeaderWithoutJoinerColumns, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, CsvConfigurations.BulkUploadCsvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();

        returnValue.ResponseClass.Should().NotBeNull();
        returnValue.ResponseClass.isDone.Should().BeTrue();

        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        parsedResult[0].organisation_id.Should().Be("23123");
        parsedResult[0].subsidiary_id.Should().Be(string.Empty);
        parsedResult[0].organisation_name.Should().Be("OrgA");
        parsedResult[0].companies_house_number.Should().Be("123456");
        parsedResult[0].parent_child.Should().Be("Parent");
        parsedResult[0].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[0].Errors.Should().BeNullOrEmpty();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].Errors.Should().BeNullOrEmpty();
    }
}