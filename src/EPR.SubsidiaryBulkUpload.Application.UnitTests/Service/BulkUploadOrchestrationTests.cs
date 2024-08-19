using EPR.SubsidiaryBulkUpload.Application.DTOs;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Service;

[TestClass]
public class BulkUploadOrchestrationTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitiaize()
    {
        fixture = new();
    }

    [TestMethod]
    public async Task ShouldProcessOrganisations()
    {
        // Arrange
        var companyData = fixture.CreateMany<CompaniesHouseCompany>();
        var parentAndSubsidiaries = fixture.CreateMany<ParentAndSubsidiaries>();
        var orgModel = fixture.Create<OrganisationResponseModel>();

        var recordExtraction = new Mock<IRecordExtraction>();
        var subsidiaryService = new Mock<ISubsidiaryService>();
        var childProcessor = new Mock<IChildProcessor>();

        recordExtraction.Setup(re => re.ExtractParentsAndChildren(companyData)).Returns(parentAndSubsidiaries);

        subsidiaryService.Setup(se => se.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(orgModel);

        var orchestrator = new BulkUploadOrchestration(recordExtraction.Object, subsidiaryService.Object, childProcessor.Object);

        var metadata = new Dictionary<string, string>();
        var userName = metadata.Where(pair => pair.Key.Contains("username"))
                         .Select(pair => pair.Value).FirstOrDefault();

        // Act
        await orchestrator.Orchestrate(companyData, metadata);

        // Assert
        foreach(var set in parentAndSubsidiaries)
        {
            childProcessor.Verify(cp => cp.Process(set.Children, set.Parent, orgModel, userName));
        }
    }
}
