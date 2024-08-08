using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using EPR.SubsidiaryBulkUpload.Application.Configs;
using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Handlers;

[ExcludeFromCodeCoverage]
public class AccountServiceAuthorisationHandler : DelegatingHandler
{
    private readonly TokenRequestContext _tokenRequestContext;

    private readonly DefaultAzureCredential? _credentials;

    public AccountServiceAuthorisationHandler(IOptions<ApiConfig> options)
    {
        if (string.IsNullOrEmpty(options.Value.AccountServiceClientId))
        {
            return;
        }

        _tokenRequestContext = new TokenRequestContext(new[] { options.Value.AccountServiceClientId });
        _credentials = new DefaultAzureCredential();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_credentials != null)
        {
            var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue(Microsoft.Identity.Web.Constants.Bearer, tokenResult.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
