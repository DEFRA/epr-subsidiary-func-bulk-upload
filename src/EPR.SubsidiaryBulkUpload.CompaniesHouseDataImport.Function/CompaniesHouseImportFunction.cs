using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.CompaniesHouseDataImport.Function;

public class CompaniesHouseImportFunction
{
    private readonly ILogger<CompaniesHouseImportFunction> _logger;

    public CompaniesHouseImportFunction(ILogger<CompaniesHouseImportFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(CompaniesHouseImportFunction))]
    public async Task Run(
        [BlobTrigger("%BlobStorage:SubsidiaryContainerName%/{name}", Connection = "BlobStorage:ConnectionString")]
        Stream stream,
        string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {Name} \n Data: {Content}", name, content);
    }
}
