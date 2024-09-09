using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Models.Submission;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Clients;

[TestClass]
public class SubmissionStatusClientTests
{
    private Fixture fixture;
    private Mock<HttpMessageHandler> httpMessageHandler;
    private HttpClient httpClient;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();

        httpMessageHandler = new Mock<HttpMessageHandler>();

        httpClient = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };
    }

    [TestMethod]
    public async Task ShouldCreateSubmissions()
    {
        // Arrange
        var expectedRequestUri = new Uri($"https://example.com/submissions");
        var submission = fixture.Create<CreateSubmission>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();
        var expectedHeaders = new Dictionary<string, string>
        {
            { "OrganisationId", apiOptions.Value.SystemOrganisationId },
            { "UserId", apiOptions.Value.SystemUserId }
        };

        httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            });

        var submissionClient = new SubmissionStatusClient(httpClient, apiOptions, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().BeSuccessful();
        httpMessageHandler.VerifyRequest(HttpMethod.Post, expectedRequestUri, expectedHeaders, Times.Once());
    }

    [TestMethod]
    public async Task ShouldReplyTimeoutWhenCreateSubmissionTimesOut()
    {
        // Arrange
        var submission = fixture.Create<CreateSubmission>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        httpMessageHandler.RespondWithException(fixture.Create<TaskCanceledException>());

        var submissionClient = new SubmissionStatusClient(httpClient, apiOptions, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.RequestTimeout);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.InsufficientStorage)]
    [DataRow(HttpStatusCode.RequestTimeout)]
    public async Task ShouldReplyHttpRequestExceptionStatus(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var submission = fixture.Create<CreateSubmission>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        var exception = new HttpRequestException("message", null, httpStatusCode);

        httpMessageHandler.RespondWithException(exception);

        var submissionClient = new SubmissionStatusClient(httpClient, apiOptions, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(httpStatusCode);
    }

    [TestMethod]
    public async Task ShouldReplyInternalServerErrorOnUnhandeledException()
    {
        // Arrange
        var submission = fixture.Create<CreateSubmission>();
        var apiOptions = fixture.CreateOptions<ApiOptions>();

        httpMessageHandler.RespondWithException(new Exception());

        var submissionClient = new SubmissionStatusClient(httpClient, apiOptions, NullLogger<SubmissionStatusClient>.Instance);

        // Act
        var responseCode = await submissionClient.CreateSubmissionAsync(submission);

        // Assert
        responseCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
