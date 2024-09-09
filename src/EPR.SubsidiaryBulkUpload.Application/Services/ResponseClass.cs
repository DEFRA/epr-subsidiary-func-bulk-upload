using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    [ExcludeFromCodeCoverage]
    public class ResponseClass
    {
        public bool isDone { get; set; }

        public string Messages { get; set; }
    }
}
