using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Models.Events;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Clients;

[TestClass]
public class SubmissionStatusClientTests
{
    private Guid _systemOrganisationId = Guid.NewGuid();
    private Guid _systemUserId = Guid.NewGuid();

    private Fixture _fixture;
    private Mock<HttpMessageHandler> _httpMessageHandler;
    private HttpClient _httpClient;
    private Mock<ISystemDetailsProvider> _systemDetailsProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new();

        _httpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        _systemDetailsProvider = new Mock<ISystemDetailsProvider>();
        _systemDetailsProvider.SetupGet(p => p.SystemOrganisationId).Returns(_systemOrganisationId);
        _systemDetailsProvider.SetupGet(p => p.SystemUserId).Returns(_systemUserId);
    }

    [TestMethod]
    public async Task ShouldCreateSubmissions()
    {
        // Arrange
        var expectedRequestUri = new Uri($"https://example.com/submissions");
        var submission = _fixture.Create<CreateSubmission>();
        var expectedHeaders = new Dictionary<string, string>
        {
            { "OrganisationId", _systemOrganisationId.ToString() },
            { "UserId", _systemUserId.ToString() }
        };

        _httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            });

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().BeSuccessful();
        _httpMessageHandler.VerifyRequest(HttpMethod.Post, expectedRequestUri, expectedHeaders, Times.Once());
    }

    [TestMethod]
    public async Task ShouldCreateAntivirusEvent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var expectedRequestUri = new Uri($"https://example.com/submissions/{submissionId}/events");

        var submission = _fixture.Create<CreateSubmission>();
        var expectedHeaders = new Dictionary<string, string>
        {
            { "OrganisationId", _systemOrganisationId.ToString() },
            { "UserId", _systemUserId.ToString() }
        };

        var blobContainerName = "test_container";
        const string fileName = "test.csv";
        var blobName = Guid.NewGuid().ToString();
        var fileId = Guid.NewGuid();

        var antivirusEvent = new AntivirusCheckEvent
        {
            FileName = fileName,
            FileType = FileType.CompaniesHouse,
            FileId = fileId,
            BlobContainerName = blobContainerName,
            RegistrationSetId = null
        };

        _httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            });

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateEventAsync(antivirusEvent, submissionId);

        // Assert
        responseCode.Should().BeSuccessful();
        _httpMessageHandler.VerifyRequest(HttpMethod.Post, expectedRequestUri, expectedHeaders, Times.Once());
    }

    [TestMethod]
    public async Task ShouldCreateSubsidiariesBulkUploadCompleteEvent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var expectedRequestUri = new Uri($"https://example.com/submissions/{submissionId}/events");

        var submission = _fixture.Create<CreateSubmission>();
        var expectedHeaders = new Dictionary<string, string>
        {
            { "OrganisationId", _systemOrganisationId.ToString() },
            { "UserId", _systemUserId.ToString() }
        };

        var blobContainerName = "test_container";
        const string fileName = "test.csv";
        var blobName = Guid.NewGuid().ToString();
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var antivirusEvent = new SubsidiariesBulkUploadCompleteEvent
        {
            BlobName = blobName,
            BlobContainerName = blobContainerName,
            FileName = fileName,
            UserId = userId
        };

        _httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            });

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateEventAsync(antivirusEvent, submissionId);

        // Assert
        responseCode.Should().BeSuccessful();
        _httpMessageHandler.VerifyRequest(HttpMethod.Post, expectedRequestUri, expectedHeaders, Times.Once());
    }

    [TestMethod]
    public async Task ShouldReplyTimeoutWhenCreateSubmissionTimesOut()
    {
        // Arrange
        var submission = _fixture.Create<CreateSubmission>();

        _httpMessageHandler.RespondWithException(_fixture.Create<TaskCanceledException>());

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.RequestTimeout);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.InsufficientStorage)]
    [DataRow(HttpStatusCode.RequestTimeout)]
    [DataRow(HttpStatusCode.BadRequest)]
    public async Task ShouldReplyHttpRequestExceptionStatus(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var submission = _fixture.Create<CreateSubmission>();

        var exception = new HttpRequestException("message", null, httpStatusCode);

        _httpMessageHandler.RespondWithException(exception);

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(httpStatusCode);
    }

    [TestMethod]
    public async Task ShouldReplyInternalServerErrorOnUnhandledException()
    {
        // Arrange
        var submission = _fixture.Create<CreateSubmission>();

        _httpMessageHandler.RespondWithException(new Exception());

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task ShouldReplyInternalServerErrorIfSystemOrganisationIdNotFound()
    {
        // Arrange
        var submission = _fixture.Create<CreateSubmission>();

        _systemDetailsProvider.SetupGet(p => p.SystemOrganisationId).Returns((Guid?)null);

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task ShouldReplyInternalServerErrorIfSystemUserIdNotFound()
    {
        // Arrange
        var submission = _fixture.Create<CreateSubmission>();

        _systemDetailsProvider.SetupGet(p => p.SystemUserId).Returns((Guid?)null);

        var submissionClient = new SubmissionStatusClient(_httpClient, _systemDetailsProvider.Object, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
