using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;
using EPR.SubsidiaryBulkUpload.Application.Services;
using EPR.SubsidiaryBulkUpload.Application.UnitTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using PollyResilience = EPR.SubsidiaryBulkUpload.Application.Resilience;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Resilience;

[TestClass]
public class PoliciesTests
{
    private const string BaseAddress = "http://any.localhost";
    private const string HttpClientName = "my-httpClient";
    private const int MaxRetries = 3;

    [TestMethod]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]

    public async Task ConfigureCompaniesHouseResilienceHandlerTest(HttpStatusCode statusCode)
    {
        // Arrange
        var services = new ServiceCollection();
        var fakeHttpDelegatingHandler = new FakeHttpDelegatingHandler(
            _ => Task.FromResult(new HttpResponseMessage(statusCode)));

        services.Configure<ApiOptions>(x =>
        {
            x.CompaniesHouseLookupBaseUrl = BaseAddress;
            x.RetryPolicyInitialWaitTime = 2;
            x.RetryPolicyMaxRetries = MaxRetries;
            x.RetryPolicyTooManyAttemptsWaitTime = 2;
            x.Timeout = 10; // Minimum value 10ms required for this
            x.TimeUnits = TimeUnit.Milliseconds;
        });

        services.AddHttpClient(HttpClientName, client =>
        {
            client.BaseAddress = new Uri(BaseAddress);
        })
                .AddPolicyHandler((services, _) => PollyResilience.Policies.GetRetryPolicy<CompaniesHouseLookupDirectService>(services))
                .AddPolicyHandler((services, _) => PollyResilience.Policies.GetTimeoutPolicy(services))
            .AddHttpMessageHandler(() => fakeHttpDelegatingHandler);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, "/any");

        // Act
        var result = await sut.SendAsync(request);

        // Assert
        result.StatusCode.Should().Be(statusCode);
        fakeHttpDelegatingHandler.Attempts.Should().Be(MaxRetries + 1);
    }
}
