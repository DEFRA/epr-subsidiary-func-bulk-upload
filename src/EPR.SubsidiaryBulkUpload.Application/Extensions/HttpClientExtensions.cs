using System.Net.Mime;
using Microsoft.Net.Http.Headers;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class HttpClientExtensions
{
    public static void AddHeaderAcceptJson(this HttpClient httpClient)
    {
        httpClient.AddDefaultRequestHeaderIfDoesNotContain(HeaderNames.Accept, MediaTypeNames.Application.Json);
    }

    private static void AddDefaultRequestHeaderIfDoesNotContain(this HttpClient httpClient, string name, string value)
    {
        if (!httpClient.DefaultRequestHeaders.Contains(name))
        {
            httpClient.DefaultRequestHeaders.Add(name, value);
        }
    }
}