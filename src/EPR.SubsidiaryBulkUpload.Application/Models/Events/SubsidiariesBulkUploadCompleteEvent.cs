namespace EPR.SubsidiaryBulkUpload.Application.Models.Events;

public class SubsidiariesBulkUploadCompleteEvent : AbstractEvent
{
    public override EventType Type => EventType.SubsidiariesBulkUploadComplete;

    public string BlobName { get; set; }

    public string BlobContainerName { get; set; }

    public string FileName { get; set; }

    public FileType FileType { get; set; }

    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }
}