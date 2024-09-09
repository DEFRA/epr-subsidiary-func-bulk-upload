using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Models.Antivirus;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services.CompaniesHouseDownload;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services.CompaniesHouseDownload;

[TestClass]
public class CompaniesHouseFilePostServiceTests
{
    private Fixture fixture;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new();
    }

    [TestMethod]
    public async Task ShouldPostFileThroughSubmissionsAndAntiVirus()
    {
        // Arrange
        var filePath = fixture.Create<Uri>().ToString();

        fixture.Customize<ApiOptions>(ctx => ctx.With(options => options.SystemUserId, Guid.NewGuid().ToString()));
        var antivirusOptions = fixture.CreateOptions<AntivirusApiOptions>();
        var blobStorageOptions = fixture.CreateOptions<BlobStorageOptions>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var submissionStatusClient = new Mock<ISubmissionStatusClient>();
        var antivirusClient = new Mock<IAntivirusClient>();

        submissionStatusClient.Setup(ssc => ssc.CreateSubmissionAsync(It.IsAny<CreateSubmission>())).ReturnsAsync(HttpStatusCode.OK);
        submissionStatusClient.Setup(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), It.IsAny<Guid>())).ReturnsAsync(HttpStatusCode.OK);
        antivirusClient.Setup(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream)).ReturnsAsync(HttpStatusCode.OK);

        var filePostService = new CompaniesHouseFilePostService(
                    submissionStatusClient.Object, antivirusClient.Object, antivirusOptions, blobStorageOptions, apiOptions);

        // Act
        var response = await filePostService.PostFileAsync(stream, filePath);

        // Assert
        response.Should().BeSuccessful();
    }

    [TestMethod]
    public async Task ShouldNotSendToAVIfCreateSubmissionFails()
    {
        // Arrange
        var filePath = fixture.Create<Uri>().ToString();
        var badResponse = HttpStatusCode.RequestTimeout;

        fixture.Customize<ApiOptions>(ctx => ctx.With(options => options.SystemUserId, Guid.NewGuid().ToString()));
        var antivirusOptions = fixture.CreateOptions<AntivirusApiOptions>();
        var blobStorageOptions = fixture.CreateOptions<BlobStorageOptions>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var submissionStatusClient = new Mock<ISubmissionStatusClient>();
        var antivirusClient = new Mock<IAntivirusClient>();

        submissionStatusClient.Setup(ssc => ssc.CreateSubmissionAsync(It.IsAny<CreateSubmission>())).ReturnsAsync(badResponse);
        submissionStatusClient.Setup(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), It.IsAny<Guid>())).ReturnsAsync(HttpStatusCode.OK);
        antivirusClient.Setup(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream)).ReturnsAsync(HttpStatusCode.OK);

        var filePostService = new CompaniesHouseFilePostService(
                    submissionStatusClient.Object, antivirusClient.Object, antivirusOptions, blobStorageOptions, apiOptions);

        // Act
        var response = await filePostService.PostFileAsync(stream, filePath);

        // Assert
        response.Should().Be(badResponse);
        submissionStatusClient.Verify(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), It.IsAny<Guid>()), Times.Never);
        antivirusClient.Verify(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream), Times.Never);
    }

    [TestMethod]
    public async Task ShouldNotSendToAVIfCreateSubmissionAntivirusEventFails()
    {
        // Arrange
        var filePath = fixture.Create<Uri>().ToString();
        var badResponse = HttpStatusCode.RequestTimeout;

        fixture.Customize<ApiOptions>(ctx => ctx.With(options => options.SystemUserId, Guid.NewGuid().ToString()));
        var antivirusOptions = fixture.CreateOptions<AntivirusApiOptions>();
        var blobStorageOptions = fixture.CreateOptions<BlobStorageOptions>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var submissionStatusClient = new Mock<ISubmissionStatusClient>();
        var antivirusClient = new Mock<IAntivirusClient>();

        submissionStatusClient.Setup(ssc => ssc.CreateSubmissionAsync(It.IsAny<CreateSubmission>())).ReturnsAsync(HttpStatusCode.OK);
        submissionStatusClient.Setup(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), It.IsAny<Guid>())).ReturnsAsync(badResponse);
        antivirusClient.Setup(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream)).ReturnsAsync(HttpStatusCode.OK);

        var filePostService = new CompaniesHouseFilePostService(
                    submissionStatusClient.Object, antivirusClient.Object, antivirusOptions, blobStorageOptions, apiOptions);

        // Act
        var response = await filePostService.PostFileAsync(stream, filePath);

        // Assert
        response.Should().Be(badResponse);
        submissionStatusClient.Verify(ssc => ssc.CreateSubmissionAsync(It.IsAny<CreateSubmission>()), Times.Once);
        antivirusClient.Verify(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream), Times.Never);
    }

    [TestMethod]
    public async Task ShouldBeBadRequestIfSystemUserIdNotGuid()
    {
        // Arrange
        var filePath = fixture.Create<Uri>().ToString();

        var antivirusOptions = fixture.CreateOptions<AntivirusApiOptions>();
        var blobStorageOptions = fixture.CreateOptions<BlobStorageOptions>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var submissionStatusClient = new Mock<ISubmissionStatusClient>();
        var antivirusClient = new Mock<IAntivirusClient>();

        var filePostService = new CompaniesHouseFilePostService(
                    submissionStatusClient.Object, antivirusClient.Object, antivirusOptions, blobStorageOptions, apiOptions);

        // Act
        var response = await filePostService.PostFileAsync(stream, filePath);

        // Assert
        response.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ShouldUseSameFileGuidForSubmissionsAndAntivirus()
    {
        // Arrange
        var filePath = fixture.Create<Uri>().ToString();
        Guid fileGuid = Guid.Empty;

        fixture.Customize<ApiOptions>(ctx => ctx.With(options => options.SystemUserId, Guid.NewGuid().ToString()));
        var antivirusOptions = fixture.CreateOptions<AntivirusApiOptions>();
        var blobStorageOptions = fixture.CreateOptions<BlobStorageOptions>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        using var stream = new MemoryStream();

        var submissionStatusClient = new Mock<ISubmissionStatusClient>();
        var antivirusClient = new Mock<IAntivirusClient>();

        submissionStatusClient.Setup(ssc => ssc.CreateSubmissionAsync(It.IsAny<CreateSubmission>()))
            .ReturnsAsync(HttpStatusCode.OK)
            .Callback<CreateSubmission>(submission => fileGuid = submission.Id);
        submissionStatusClient.Setup(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), It.IsAny<Guid>())).ReturnsAsync(HttpStatusCode.OK);
        antivirusClient.Setup(avc => avc.SendFileAsync(It.IsAny<FileDetails>(), filePath, stream)).ReturnsAsync(HttpStatusCode.OK);

        var filePostService = new CompaniesHouseFilePostService(
                    submissionStatusClient.Object, antivirusClient.Object, antivirusOptions, blobStorageOptions, apiOptions);

        // Act
        var response = await filePostService.PostFileAsync(stream, filePath);

        // Assert
        response.Should().BeSuccessful();
        submissionStatusClient.Verify(ssc => ssc.CreateEventAsync(It.IsAny<AntivirusCheckEvent>(), fileGuid));
        antivirusClient.Verify(avc => avc.SendFileAsync(It.Is<FileDetails>(fd => fd.Key == fileGuid), filePath, stream));
    }
}
