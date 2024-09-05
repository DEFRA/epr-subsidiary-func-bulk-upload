using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules don't yet support collection expressions")]
public class ParserClassTests
{
    private Fixture _fixture;
    private CsvConfiguration _csvConfiguration;
    private string _csvHeader;
    private List<CompaniesHouseCompany> _listDataModel = null;
    private Mock<ILogger<ParserClass>> _loggerMock;
    private ParserClass _sut;

    [TestInitialize]
    public void ParserClassMockObjectsSetup()
    {
        _fixture = new();

        _loggerMock = new Mock<ILogger<ParserClass>>();

        _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        _csvHeader = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
        _listDataModel = new List<CompaniesHouseCompany>
            {
                new() { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = string.Empty },
                new() { organisation_id = "23200", subsidiary_id = "Sub1", organisation_name = "OrgB", companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = "License123", Errors = string.Empty }
            };

        _sut = new ParserClass(_loggerMock.Object);
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnEmptyData()
    {
        string[] all = [_csvHeader];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var returnValue = _sut.ParseWithHelper(stream, _csvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().BeEmpty();
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData()
    {
        // Arrange
        var rawSource = _listDataModel.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");
        string[] all = [_csvHeader, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        // Act
        var returnValue = _sut.ParseWithHelper(stream, _csvConfiguration);

        // Assert
        returnValue.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Should().NotBeNull();
        returnValue.CompaniesHouseCompany.Count.Should().Be(2);

        var parsedResult = returnValue.CompaniesHouseCompany;

        Assert.AreEqual("23123", parsedResult[0].organisation_id);
        Assert.AreEqual(string.Empty, parsedResult[0].subsidiary_id);
        Assert.AreEqual("OrgA", parsedResult[0].organisation_name);
        Assert.AreEqual("123456", parsedResult[0].companies_house_number);
        Assert.AreEqual("Parent", parsedResult[0].parent_child);
        Assert.AreEqual(string.Empty, parsedResult[0].franchisee_licensee_tenant);

        Assert.AreEqual("23200", parsedResult[1].organisation_id);
        Assert.AreEqual("Sub1", parsedResult[1].subsidiary_id);
        Assert.AreEqual("OrgB", parsedResult[1].organisation_name);
        Assert.AreEqual("654321", parsedResult[1].companies_house_number);
        Assert.AreEqual("Child", parsedResult[1].parent_child);
        Assert.AreEqual("License123", parsedResult[1].franchisee_licensee_tenant);
    }
}