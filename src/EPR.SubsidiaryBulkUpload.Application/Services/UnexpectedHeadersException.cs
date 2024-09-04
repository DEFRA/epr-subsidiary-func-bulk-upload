using CsvHelper;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class UnexpectedHeadersException : ValidationException
{
    public UnexpectedHeadersException(CsvContext context, List<string> unexpectedHeaders)
        : base(context, $"Unexpected headers: {string.Join(", ", unexpectedHeaders.Select(h => $"'{h}'"))}")
    {
        this.UnexpectedHeaders = unexpectedHeaders;
    }

    public List<string> UnexpectedHeaders { get; }
}
