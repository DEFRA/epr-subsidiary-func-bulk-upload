using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace EPR.SubsidiaryBulkUpload.Application.Exceptions;

[Serializable]
[ExcludeFromCodeCoverage(Justification ="Exceptions are not tested; Their use is confirmed in service tests")]
public class ProblemResponseException : Exception
{
    public ProblemResponseException(ProblemDetails problemDetailsDetails, HttpStatusCode statusCode)
        : base($"Problem response received: StatusCode = {(int)statusCode}, Type = {problemDetailsDetails?.Type}, Detail = {problemDetailsDetails?.Detail}")
    {
        ProblemDetails = problemDetailsDetails;
        StatusCode = statusCode;
    }

    public ProblemResponseException()
    {
    }

    public ProblemResponseException(string message)
        : base(message)
    {
    }

    public ProblemResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected ProblemResponseException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        : base(
            info: info, context)
    {
    }
#pragma warning restore SYSLIB0051 // Type or member is obsolete

    public ProblemDetails ProblemDetails { get; }

    public HttpStatusCode StatusCode { get; }
}