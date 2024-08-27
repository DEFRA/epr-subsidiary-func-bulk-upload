using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Service;

[TestClass]
public class CsvProcessorTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitiaize()
    {
        fixture = new();
    }

    [TestMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Style cop rules dont yet support collection expressions")]
    public async Task ShouldProcessCompaniesHouseScvUploadData()
    {
        // Arrange
        var source = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        var header = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant\n";
        var rawSource = source.Select(s => $"{s.organisation_id},{s.subsidiary_id},{s.organisation_name},{s.companies_house_number},{s.parent_child},{s.franchisee_licensee_tenant}\n");

        string[] all = [header, ..rawSource];

        using var stream = new MemoryStream(all.SelectMany(s => Encoding.UTF8.GetBytes(s)).ToArray());

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        var processor = new CsvProcessor(null, NullLogger<CsvProcessor>.Instance);

        // Act
        var actual = (await processor.ProcessStream<CompaniesHouseCompany, CompaniesHouseCompanyMap>(stream, configuration)).ToArray();

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
}
