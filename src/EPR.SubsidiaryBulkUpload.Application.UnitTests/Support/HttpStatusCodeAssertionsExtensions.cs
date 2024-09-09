using System.Net;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;

public static class HttpStatusCodeAssertionsExtensions
{
    public static HttpStatusCodeAssertions Should(this HttpStatusCode statusCode)
    {
        return new HttpStatusCodeAssertions(statusCode);
    }
}
