namespace EPR.SubsidiaryBulkUpload.Application.Models;

public record FilePart(int PartNumber, int TotalFiles, string PartitionKey)
{
}
