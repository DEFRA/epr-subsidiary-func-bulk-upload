using System.Text;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules don't yet support collection expressions")]
public class ParserClassTests
{
    private const string _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
    private const string _csvHeaderWithMissingSubsidiaryId = "organisation_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
    private const string _badHeader = "organisation,subsidiary,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";

    private Fixture _fixture;
    private List<CompaniesHouseCompany> _listDataModel = null;
    private Mock<ILogger<ParserClass>> _loggerMock;
    private ParserClass _sut;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();

        _loggerMock = new Mock<ILogger<ParserClass>>();

        _listDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = string.Empty },
                new() { organisation_id = "23123", subsidiary_id = "Sub1", organisation_name = "OrgB", companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = string.Empty, Errors = string.Empty }
            };

        _sut = new ParserClass(_loggerMock.Object);
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
        var rawSource = _listDataModel.Take(1).Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
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

        errorRow.UploadFileErrorModel.Should().NotBeNull();
        errorRow.UploadFileErrorModel.FileContent.Should().Be("headererror-Invalid");
        errorRow.UploadFileErrorModel.Message.Should().Contain("organisation_id");
        errorRow.UploadFileErrorModel.Message.Should().Contain("subsidiary_id");
    }

    [TestMethod]
    public void ParseClass_Missing_Column_ReturnsError()
    {
        var rawSource = _listDataModel.Take(1).Select(s => $"{s.organisation_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
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

        errorRow.UploadFileErrorModel.Should().NotBeNull();
        errorRow.UploadFileErrorModel.FileContent.Should().Be("headererror-Invalid");
        errorRow.UploadFileErrorModel.Message.Should().Contain("subsidiary_id");
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData()
    {
        // Arrange
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
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
        parsedResult[0].UploadFileErrorModel.Should().BeNull();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be("OrgB");
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be(string.Empty);
        parsedResult[1].UploadFileErrorModel.Should().BeNull();
    }

    [TestMethod]
    public void ParseClass_InvalidData_ReturnsErrorMessage()
    {
        // Arrange
        var badDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = string.Empty },
                new() { organisation_id = "23123", subsidiary_id = "Sub1", organisation_name = string.Empty, companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = "License123", Errors = string.Empty }
            };

        var rawSource = badDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
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
        parsedResult[0].UploadFileErrorModel.Should().BeNull();

        parsedResult[1].organisation_id.Should().Be("23123");
        parsedResult[1].subsidiary_id.Should().Be("Sub1");
        parsedResult[1].organisation_name.Should().Be(string.Empty);
        parsedResult[1].companies_house_number.Should().Be("654321");
        parsedResult[1].parent_child.Should().Be("Child");
        parsedResult[1].franchisee_licensee_tenant.Should().Be("License123");

        parsedResult[1].UploadFileErrorModel.Should().NotBeNull();
        parsedResult[1].UploadFileErrorModel.Message.Should().Contain("organisation_name is required.");
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
}