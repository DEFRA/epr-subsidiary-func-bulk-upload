using System.Text;
using EPR.SubsidiaryBulkUpload.Application.ClassMaps;
using EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class CsvProcessorTests
{
    private Fixture _fixture;
    private Mock<ILogger<CsvProcessor>> _mockLogger;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
        _mockLogger = new Mock<ILogger<CsvProcessor>>();
    }

    [TestMethod]
    public async Task ShouldProcessCompaniesHouseScvUploadData()
    {
        // Arrange
        var source = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        var header = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
        var rawSource = source.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");

        string[] all = [header, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var configuration = CsvConfigurations.BulkUploadCsvConfiguration;

        var parserClass = new Mock<IParserClass>();
        parserClass
            .Setup(p => p.ParseWithHelper(It.IsAny<Stream>(), configuration))
            .Returns((new ResponseClass(), source.ToList()));

        var processor = new CsvProcessor(parserClass.Object, NullLogger<CsvProcessor>.Instance);

        // Act
        var actual = (await processor.ProcessStreamWithMapping<CompaniesHouseCompany, CompaniesHouseCompanyMap>(stream, configuration)).ToArray();

        // Assert
        actual.Should().HaveCount(2);

        actual.Should().Contain(chc => chc.organisation_id == source[0].organisation_id &&
                               chc.subsidiary_id == source[0].subsidiary_id &&
                               chc.organisation_name == source[0].organisation_name &&
                               chc.companies_house_number == source[0].companies_house_number &&
                               chc.parent_child == source[0].parent_child &&
                               chc.franchisee_licensee_tenant == source[0].franchisee_licensee_tenant);

        actual.Should().Contain(chc => chc.organisation_id == source[1].organisation_id &&
                               chc.subsidiary_id == source[1].subsidiary_id &&
                               chc.organisation_name == source[1].organisation_name &&
                               chc.companies_house_number == source[1].companies_house_number &&
                               chc.parent_child == source[1].parent_child &&
                               chc.franchisee_licensee_tenant == source[1].franchisee_licensee_tenant);
    }

    [TestMethod]
    public async Task ShouldLogIfParsingThrows()
    {
        // Arrange
        var source = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        var header = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
        var rawSource = source.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");

        string[] all = [header, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var configuration = CsvConfigurations.BulkUploadCsvConfiguration;

        var exception = new Exception("Test Exception");

        var parserClass = new Mock<IParserClass>();
        parserClass
            .Setup(p => p.ParseWithHelper(It.IsAny<Stream>(), configuration)).Throws(exception);

        var processor = new CsvProcessor(parserClass.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<Exception>(() =>
            processor.ProcessStreamWithMapping<CompaniesHouseCompany, CompaniesHouseCompanyMap>(stream, configuration));

        // Assert
        _mockLogger.Verify(
            logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            exception,
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [TestMethod]
    public async Task ShouldProcessCsvStream()
    {
        // Arrange
        var source = _fixture.CreateMany<CompanyHouseTableEntity>(2).ToArray();
        var header = "CompanyName,CompanyNumber,CompanyStatus\n";

        var rawSource = source.Select(s => $"{s.CompanyName},{s.CompanyNumber},{s.CompanyStatus}\n");

        string[] all = [header, .. rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var processor = new CsvProcessor(null, NullLogger<CsvProcessor>.Instance);

        // Act
        var actual = (await processor.ProcessStream<CompanyHouseTableEntity>(stream, CsvConfigurations.BulkUploadCsvConfiguration)).ToArray();

        // Assert
        actual.Should().HaveCount(2);

        actual.Should().Contain(chc => chc.CompanyName == source[0].CompanyName &&
                               chc.CompanyNumber == source[0].CompanyNumber &&
                               chc.CompanyStatus == source[0].CompanyStatus);

        actual.Should().Contain(chc => chc.CompanyName == source[1].CompanyName &&
                               chc.CompanyNumber == source[1].CompanyNumber &&
                               chc.CompanyStatus == source[1].CompanyStatus);
    }

    [TestMethod]
    public async Task ShouldProcessCsvStream_IgnoringEmptyRows()
    {
        // Arrange
        var source = _fixture.CreateMany<CompanyHouseTableEntity>(2).ToArray();
        var header = "CompanyName,CompanyNumber,CompanyStatus\n";

        var rawSource = source.Select(s => $"{s.CompanyName},{s.CompanyNumber},{s.CompanyStatus}\n").ToArray();

        string[] all = [
            header,
            rawSource[0],
            rawSource[1],
            "\r\n",
            "     \r\n",
            "\r\n"
            ];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var processor = new CsvProcessor(null, NullLogger<CsvProcessor>.Instance);

        // Act
        var actual = (await processor.ProcessStream<CompanyHouseTableEntity>(stream, CsvConfigurations.BulkUploadCsvConfiguration)).ToArray();

        // Assert
        actual.Should().HaveCount(3);

        actual.Should().Contain(chc => chc.CompanyName == source[0].CompanyName &&
                               chc.CompanyNumber == source[0].CompanyNumber &&
                               chc.CompanyStatus == source[0].CompanyStatus);

        actual.Should().Contain(chc => chc.CompanyName == source[1].CompanyName &&
                               chc.CompanyNumber == source[1].CompanyNumber &&
                               chc.CompanyStatus == source[1].CompanyStatus);

        actual.Should().Contain(chc => chc.CompanyName == string.Empty &&
                               chc.CompanyNumber == string.Empty &&
                               chc.CompanyStatus == string.Empty);
    }
}