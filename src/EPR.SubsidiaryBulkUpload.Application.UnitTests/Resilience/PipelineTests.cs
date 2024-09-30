using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Moq.Protected;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Resilience;

[TestClass]
public class PipelineTests
{
    private const string BaseAddress = "http://any.localhost";
    private const string HttpClientName = "my-httpClient";
    private const int MaxRetries = 2;
    private const int MaxRetriesFor429 = 3;

    private readonly Mock<DelegatingHandler> _httpMessageHandlerMock = new();

    [TestMethod]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task ConfigureCompaniesHouseResilienceHandlerTest_For_TransientError(HttpStatusCode statusCode)
    {
        // Arrange
        var attempts = 0;
        var services = new ServiceCollection();

        services.Configure<ApiOptions>(x =>
        {
            x.CompaniesHouseLookupBaseUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 1;
            x.RetryPolicyMaxRetries = MaxRetries;
            x.RetryPolicyTooManyAttemptsWaitTime = 1;
            x.RetryPolicyTooManyAttemptsMaxRetries = MaxRetriesFor429;
            x.Timeout = 10; // Minimum value 10ms required for this
            x.TimeUnits = TimeUnit.Milliseconds;
        });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode))
            .Callback<HttpRequestMessage, CancellationToken>((m, c) => attempts++);

        services.AddHttpClient(HttpClientName, client =>
        {
            client.BaseAddress = new Uri(BaseAddress);
        })
            .AddCompaniesHouseResilienceHandlerToHttpClientBuilder()
            .AddHttpMessageHandler(() => _httpMessageHandlerMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, "/any");

        // Act
        var result = await sut.SendAsync(request);

        // Assert
        result.StatusCode.Should().Be(statusCode);
        attempts.Should().Be(MaxRetries + 1);
    }

    [TestMethod]
    public async Task ConfigureCompaniesHouseResilienceHandlerTest_For_TooManyRequests()
    {
        // Arrange
        var attempts = 0;
        var statusCode = HttpStatusCode.TooManyRequests;

        var services = new ServiceCollection();

        services.Configure<ApiOptions>(x =>
        {
            x.CompaniesHouseLookupBaseUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 1;
            x.RetryPolicyMaxRetries = MaxRetries;
            x.RetryPolicyTooManyAttemptsWaitTime = 1;
            x.RetryPolicyTooManyAttemptsMaxRetries = MaxRetriesFor429;
            x.Timeout = 10; // Minimum value 10ms required for this
            x.TimeUnits = TimeUnit.Milliseconds;
        });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode))
            .Callback<HttpRequestMessage, CancellationToken>((m, c) => attempts++);

        services.AddHttpClient(HttpClientName, client =>
        {
            client.BaseAddress = new Uri(BaseAddress);
        })
            .AddCompaniesHouseResilienceHandlerToHttpClientBuilder()
            .AddHttpMessageHandler(() => _httpMessageHandlerMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, "/any");

        // Act
        var result = await sut.SendAsync(request);

        // Assert
        result.StatusCode.Should().Be(statusCode);
        attempts.Should().BeGreaterThanOrEqualTo(MaxRetriesFor429 + 1);
    }
}
