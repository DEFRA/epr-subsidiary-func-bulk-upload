using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class CsvErrorModel
{
    public InnerExceptionResponse? InnerException { get; init; }

    /// <summary>
    /// Gets or sets error row number in the file {rownumber}.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1609:Property documentation should have value", Justification = "summary requried")]
    public string RowNumber { get; set; }

    /// <summary>
    /// Gets or sets error field in the file {rownumber}.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1609:Property documentation should have value", Justification = "summary requried")]
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets raw Row Record.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1609:Property documentation should have value", Justification = "summary requried")]
    public string FullRow { get; set; }

    public string ErrorMessage { get; set; }
}