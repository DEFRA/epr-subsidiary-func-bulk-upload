using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules dont yet support collection expressions")]
public class ParserClassTests
{
    private Fixture fixture;
    private List<CompaniesHouseCompany> _listDataModel = null;
    private Mock<ILogger<ParserClass>> _loggerMock;
    private string filePath = "testing.csv";
    private Mock<IParserClass> parserClassMoq = null;

    [ClassInitialize]
    public void ParserClassRequiredObjects(TestContext context)
    {
        File.WriteAllText(filePath, "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n1,23123,,OrgA,123456,Parent,,\n2,23200,Sub1,OrgB,654321,Child,License123");

        _listDataModel = new List<CompaniesHouseCompany>
            {
                new CompaniesHouseCompany { organisation_id = "23123",  subsidiary_id = string.Empty, organisation_name = "OrgA", companies_house_number = "123456", parent_child = "Parent", franchisee_licensee_tenant = string.Empty, Errors = string.Empty },
                new CompaniesHouseCompany { organisation_id = "23200", subsidiary_id = "Sub1", organisation_name = "OrgB", companies_house_number = "654321", parent_child = "Child", franchisee_licensee_tenant = "License123", Errors = string.Empty }
            };

        _loggerMock = new Mock<ILogger<ParserClass>>();
    }

    [TestInitialize]
    public void ParserClassMockObjectsSetup()
    {
        _loggerMock = new Mock<ILogger<ParserClass>>();

        parserClassMoq = new Mock<IParserClass>();
        parserClassMoq.Setup(x => x.ParseWithHelper (filePath)).Returns(_listDataModel);

        parserClassMoq.Setup(x => x.Parse(filePath)).Returns((List<CompaniesHouseCompany>)Enumerable.Empty<List<CompaniesHouseCompany>>());
        parserClassMoq.Setup(x => x.Parse(filePath)).Returns(new List<CompaniesHouseCompany>());

        parserClassMoq.Setup(x => x.Parse(filePath)).Throws(new Exception("Error reading CSV file"));
    }

    [ClassCleanup]
    public void ParserClassCleanStaticObjects()
    {
        File.Delete(filePath);
    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnEmptyData()
    {
        var theResult = parserClassMoq.Object.Parse(filePath);

        //Assert.That.

        Assert.IsTrue(theResult.Count().Equals(0));

    }

    [TestMethod]
    public void ParseClass_ValidCsvFile_ReturnsCorrectData()
    {
        // Arrange
        ParserClass sut = new ParserClass(_loggerMock.Object);

        // Act
        var returnValue = sut.Parse(filePath);

        // Assert
        returnValue.Should().BeOfType<List<CompaniesHouseCompany>>();

        Assert.AreEqual(2, returnValue.Count); // Two rows

        Assert.AreEqual(1, returnValue[0].organisation_id);
        Assert.AreEqual(string.Empty, returnValue[0].suborg_id);
        Assert.AreEqual("OrgA", returnValue[0].organisation_name);
        Assert.AreEqual("123456", returnValue[0].organisation_number);
        Assert.AreEqual("Parent", returnValue[0].parent_or_child);
        Assert.AreEqual("", returnValue[0].license);

        Assert.AreEqual(2, returnValue[1].organisation_id);
        Assert.AreEqual("Sub1", returnValue[1].suborg_id);
        Assert.AreEqual("OrgB", returnValue[1].organisation_name);
        Assert.AreEqual("654321", returnValue[1].organisation_number);
        Assert.AreEqual("Child", returnValue[1].parent_or_child);
        Assert.AreEqual("License123", returnValue[1].license);

        //// Cleanup
        // File.Delete(filePath);
    }

    [TestMethod]
    public void ParseClass_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var parserClass = new ParserClass(_loggerMock.Object);
        var filePath = "nonexistent.csv";

        // Act & Assert
        Assert.ThrowsException<FileNotFoundException>(() => parserClass.ParseWithHelper(filePath));
    }
}