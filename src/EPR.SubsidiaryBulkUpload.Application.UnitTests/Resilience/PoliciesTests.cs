using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Clients;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq.Protected;
using PollyResilience = EPR.SubsidiaryBulkUpload.Application.Resilience;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Resilience;

[TestClass]
public class PoliciesTests
{
    private const string BaseAddress = "http://any.localhost";
    private const string HttpClientName = "my-httpClient";
    private const int MaxRetries = 2;

    private readonly Mock<DelegatingHandler> _httpMessageHandlerMock = new();

    [TestMethod]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task DefaultRetryPoliciesTest(HttpStatusCode statusCode)
    {
        // Arrange
        var attempts = 0;
        var services = new ServiceCollection();

        services.Configure<ApiOptions>(x =>
        {
            x.CompaniesHouseLookupBaseUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 1;
            x.RetryPolicyMaxRetries = MaxRetries;
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
            .AddPolicyHandler((services, _) => PollyResilience.Policies.DefaultRetryPolicy<CompaniesHouseLookupDirectService>(services))
            .AddPolicyHandler((services, _) => PollyResilience.Policies.DefaultTimeoutPolicy(services))
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
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task AntivirusRetryPoliciesTest(HttpStatusCode statusCode)
    {
        // Arrange
        var attempts = 0;
        var services = new ServiceCollection();

        services.Configure<AntivirusApiOptions>(x =>
        {
            x.BaseUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 1;
            x.RetryPolicyMaxRetries = MaxRetries;
            x.Timeout = 10;
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
            .AddPolicyHandler((services, _) => PollyResilience.Policies.AntivirusRetryPolicy<AntivirusClient>(services))
            .AddPolicyHandler((services, _) => PollyResilience.Policies.AntivirusTimeoutPolicy(services))
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
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task CompaniesHouseDownloadRetryPoliciesTest(HttpStatusCode statusCode)
    {
        // Arrange
        var attempts = 0;
        var services = new ServiceCollection();

        services.Configure<CompaniesHouseDownloadOptions>(x =>
        {
            x.DataDownloadUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 1;
            x.RetryPolicyMaxRetries = MaxRetries;
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
            .AddPolicyHandler((services, _) => PollyResilience.Policies.CompaniesHouseDownloadRetryPolicy<CompaniesHouseLookupDirectService>(services))
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
}
