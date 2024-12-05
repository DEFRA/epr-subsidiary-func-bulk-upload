using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

[TestClass]
public class SystemDetailsProviderTests
{
    private Guid _systemUserId = Guid.NewGuid();
    private Guid _systemOrganisationId = Guid.NewGuid();

    private Mock<ISubsidiaryService> _subsidiaryService;
    private SystemDetailsProvider _sut;

    [TestInitialize]
    public void TestInitialize()
    {
        _subsidiaryService = new Mock<ISubsidiaryService>();

        _subsidiaryService
            .Setup(s => s.GetSystemUserAndOrganisation())
            .ReturnsAsync(new UserOrganisation
            {
                OrganisationId = _systemOrganisationId,
                UserId = _systemUserId
            });

        _sut = new SystemDetailsProvider(_subsidiaryService.Object);
    }

    [TestMethod]
    public async Task Should_Return_SystemUserId()
    {
        // Act
        var returnedSystemUserId = _sut.SystemUserId;

        // Assert
        returnedSystemUserId.Should().Be(_systemUserId);
    }

    [TestMethod]
    public async Task Should_Return_SystemOrganisationId()
    {
        // Act
        var returnedSystemOrganisationId = _sut.SystemOrganisationId;

        // Assert
        returnedSystemOrganisationId.Should().Be(_systemOrganisationId);
    }

    [TestMethod]
    public async Task Should_Call_SystemDetails_Once()
    {
        // Act
        var returnedSystemUserId1 = _sut.SystemUserId;
        var returnedSystemUserId2 = _sut.SystemUserId;
        var returnedSystemOrganisationId = _sut.SystemOrganisationId;
        var returnedSystemOrganisationId2 = _sut.SystemOrganisationId;

        // Assert
        _subsidiaryService
            .Verify(s => s.GetSystemUserAndOrganisation(), Times.Once);
    }
}
