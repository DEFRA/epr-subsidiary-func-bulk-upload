using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using EPR.SubsidiaryBulkUpload.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Handlers;

[ExcludeFromCodeCoverage]
public class CompaniesHouseCredentialHandler : DelegatingHandler
{
    private readonly TokenRequestContext _tokenRequestContext;
    private readonly ClientSecretCredential _credentials;

    public CompaniesHouseCredentialHandler(IOptions<ApiOptions> options)
    {
        var clientApiOptions = options.Value;
        _tokenRequestContext = new TokenRequestContext(new[] { clientApiOptions.CompaniesHouseScope });
        _credentials = new ClientSecretCredential(clientApiOptions.TenantId, clientApiOptions.ClientId, clientApiOptions.ClientSecret);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Client.Bearer, tokenResult.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}
