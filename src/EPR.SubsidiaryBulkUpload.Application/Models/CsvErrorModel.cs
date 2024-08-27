using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class CsvErrorModel
{
    public InnerExceptionResponse? InnerException { get; init; }

    public string RowNumber { get; set; }

    public string FieldName { get; set; }

    public string FullRow { get; set; }

    public string ErrorMessage { get; set; }
}