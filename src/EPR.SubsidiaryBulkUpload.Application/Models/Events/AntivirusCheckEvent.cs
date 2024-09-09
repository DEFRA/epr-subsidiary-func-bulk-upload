namespace EPR.SubsidiaryBulkUpload.Application.Models.Events;

public class AntivirusCheckEvent
{
    public EventType Type => EventType.AntivirusCheck;

    public Guid FileId { get; set; }

    public string FileName { get; set; }

    public FileType FileType { get; set; }

    public Guid? RegistrationSetId { get; set; }

    public string? BlobContainerName { get; set; }
}