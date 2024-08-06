using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class InnerExceptionResponse
{
    public HttpStatusCode? StatusCode => Code != null ? (HttpStatusCode)int.Parse(Code) : null;

    public string? Code { get; init; }
}
