using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Exceptions;

[ExcludeFromCodeCoverage(Justification ="Exceptions are not tested; Their use is confirmed in service tests")]
public class MissingSystemDetailsException : Exception
{
    public MissingSystemDetailsException()
    {
    }

    public MissingSystemDetailsException(string message)
        : base(message)
    {
    }

    public MissingSystemDetailsException(string message, Exception inner)
        : base(message, inner)
    {
    }
}