using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Services;
using FluentAssertions;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Service;

[TestClass]
public class RecordExtractionTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitiaize()
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

        var parent1Children = fixture.CreateMany<CompaniesHouseCompany>();

        fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[1].organisation_id));
        var parent2Children = fixture.CreateMany<CompaniesHouseCompany>();

        var all = Enumerable.Concat(parents, parent1Children).Concat(parent2Children);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndChildren(all);

        // Assert
        parentAndSubsidiaries.Should().HaveCount(2);
        var parent1AndChildren = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[0].organisation_id);
        parent1Children.Should().BeEquivalentTo(parent1Children);
        var parent2AndChildren = parentAndSubsidiaries.First(ps => ps.Parent.organisation_id == parents[1].organisation_id);
        parent2Children.Should().BeEquivalentTo(parent2Children);
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoParents()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var children = fixture.CreateMany<CompaniesHouseCompany>();

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndChildren(children);

        // Assert
        parentAndSubsidiaries.Should().BeEmpty();
    }

    [TestMethod]
    public void ShouldIgnoreRecordsWhereThereAreNoChildren()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>();

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndChildren(parents);

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
        var children = fixture.CreateMany<CompaniesHouseCompany>();

        var all = Enumerable.Concat(parents, children);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndChildren(all);

        // Assert
        parentAndSubsidiaries.Should().BeEmpty();
    }

    [TestMethod]
    public void ShouldExtractOnlyParentsWithChildren()
    {
        // Arrange
        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Parent"));
        var parents = fixture.CreateMany<CompaniesHouseCompany>(2).ToArray();

        fixture.Customize<CompaniesHouseCompany>(ctx => ctx.With(chc => chc.parent_child, "Child"));
        var miscChildren = fixture.CreateMany<CompaniesHouseCompany>();

        fixture.Customize<CompaniesHouseCompany>(ctx =>
            ctx.With(chc => chc.parent_child, "Child")
               .With(chc => chc.organisation_id, parents[0].organisation_id));

        var parent1Children = fixture.CreateMany<CompaniesHouseCompany>();

        var all = Enumerable.Concat(parents, miscChildren).Concat(parent1Children);

        var extraction = new RecordExtraction();

        // Act
        var parentAndSubsidiaries = extraction.ExtractParentsAndChildren(all).ToArray();

        // Assert
        parentAndSubsidiaries.Should().HaveCount(1);
        parentAndSubsidiaries[0].Parent.Should().BeEquivalentTo(parents[0]);
        parentAndSubsidiaries[0].Children.Should().BeEquivalentTo(parent1Children);
    }
}
