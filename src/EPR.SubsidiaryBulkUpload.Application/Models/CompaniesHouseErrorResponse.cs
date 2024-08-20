using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]

public class CompaniesHouseErrorResponse
{
    public InnerExceptionResponse? InnerException { get; init; }
}
