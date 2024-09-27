namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.TestHelpers;

public class FakeHttpDelegatingHandler(Func<int, Task<HttpResponseMessage>> responseFactory) : DelegatingHandler
{
    private readonly Func<int, Task<HttpResponseMessage>> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));

    public int Attempts { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _responseFactory.Invoke(++Attempts);
    }
}
