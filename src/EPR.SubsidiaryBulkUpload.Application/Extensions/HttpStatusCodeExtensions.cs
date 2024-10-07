using System.Net;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class HttpStatusCodeExtensions
{
    public static bool IsSuccessStatusCode(this HttpStatusCode statusCode)
    {
        return ((int)statusCode >= 200) && ((int)statusCode <= 299);
    }

    public static async Task<HttpStatusCode> ThenIfIsSuccessStatusCode(this Task<HttpStatusCode> first, Func<Task<HttpStatusCode>> second)
    {
        var statusCode = await first;

        if (IsSuccessStatusCode(statusCode))
        {
            statusCode = await second();
        }

        return statusCode;
    }
}