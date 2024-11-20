using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class RecordExtractionTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
    }

    [TestMethod]
    public void ShouldExtractParentsAndChildren()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[0].organisation_id));
        var parent1Subsidiaries = fixture.CreateMany<CompaniesHouseCompany>();

        fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[1].organisation_id));
        var parent2Subsidiaries = fixture.CreateMany<CompaniesHouseCompany>();

        var all = parents.Concat(parent1Subsidiaries).Concat(parent2Subsidiaries);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all);

        // Assert
        parentAndSubsidiaries.Should().HaveCount(2);
        var parent1AndSubsidiaries = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[0].organisation_id);
        parent1AndSubsidiaries.Subsidiaries.Should().BeEquivalentTo(parent1Subsidiaries);
        var parent2AndSubsidiaries = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[1].organisation_id);
        parent2AndSubsidiaries.Subsidiaries.Should().BeEquivalentTo(parent2Subsidiaries);
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoParents()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var subsidiaries = fixture.CreateMany<CompaniesHouseCompany>();

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
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>();

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(parents);

        // Assert
        parentAndSubsidiaries.Should().BeEmpty();
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoParentsWithChildren()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>();
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var subsidiaries = fixture.CreateMany<CompaniesHouseCompany>();

        var all = parents.Concat(subsidiaries);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(3);
        parentAndSubsidiaries[0].Parent.organisation_name.Should().BeEquivalentTo("orphan");
        parentAndSubsidiaries[1].Parent.organisation_name.Should().BeEquivalentTo("orphan");
    }

    [TestMethod]
    public void ShouldExtractOnlyParentsWithChildren()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var miscSubsidiaries = fixture.CreateMany<CompaniesHouseCompany>();

        fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[0].organisation_id));

        var parent1Children = fixture.CreateMany<CompaniesHouseCompany>();

        var all = parents.Concat(miscSubsidiaries).Concat(parent1Children);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndSubsidiaries(all).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(4);
        parentAndSubsidiaries[0].Parent.Should().BeEquivalentTo(parents[0]);
        parentAndSubsidiaries[0].Subsidiaries.Should().BeEquivalentTo(parent1Children);
    }
}
