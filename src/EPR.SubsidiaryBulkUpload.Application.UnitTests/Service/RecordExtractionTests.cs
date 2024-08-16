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
}
