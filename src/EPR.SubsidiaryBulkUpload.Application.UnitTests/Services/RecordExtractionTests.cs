using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class RecordExtractionTests
{
    private Fixture _fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();
    }

    [TestMethod]
    public void ShouldExtractParentsAndChildren()
    {
        // Arrange
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        _fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[0].organisation_id));
        var parent1Subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1);

        _fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[1].organisation_id));
        var parent2Subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>(1);

        var all = parents.Concat(parent1Subsidiaries).Concat(parent2Subsidiaries);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all);

        // Assert
        parentAndSubsidiaries.Should().HaveCount(2);
        var parent1AndSubsidiaries = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[0].organisation_id);
        parent1AndSubsidiaries.Subsidiaries.Count.Should().Be(1);
        var parent2AndSubsidiaries = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[1].organisation_id);
        parent2AndSubsidiaries.Subsidiaries.Count.Should().Be(1);
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoParents()
    {
        // Arrange
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>();

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(subsidiaries).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(3);
        parentAndSubsidiaries[0].Parent.organisation_name.Should().BeEquivalentTo("orphan");
        parentAndSubsidiaries[1].Parent.organisation_name.Should().BeEquivalentTo("orphan");
        parentAndSubsidiaries[2].Parent.organisation_name.Should().BeEquivalentTo("orphan");
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoSubsidiaries()
    {
        // Arrange
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = _fixture.CreateMany<CompaniesHouseCompany>();

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(parents).ToArray();

        // Assert
        parentAndSubsidiaries[0].Subsidiaries.Count.Should().Be(1);
        parentAndSubsidiaries[1].Subsidiaries.Count.Should().Be(1);
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoParentsWithChildren()
    {
        // Arrange
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = _fixture.CreateMany<CompaniesHouseCompany>();
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var subsidiaries = _fixture.CreateMany<CompaniesHouseCompany>();

        var all = parents.Concat(subsidiaries);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(6);
        parentAndSubsidiaries[3].Parent.organisation_name.Should().BeEquivalentTo("orphan");
        parentAndSubsidiaries[3].Parent.organisation_name.Should().BeEquivalentTo("orphan");
    }

    [TestMethod]
    public void ShouldExtractOnlyParentsWithChildren()
    {
        // Arrange
        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = _fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        _fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var miscSubsidiaries = _fixture.CreateMany<CompaniesHouseCompany>();

        _fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[0].organisation_id));

        var parent1Children = _fixture.CreateMany<CompaniesHouseCompany>();

        var all = parents.Concat(miscSubsidiaries).Concat(parent1Children);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(5);
    }
}
