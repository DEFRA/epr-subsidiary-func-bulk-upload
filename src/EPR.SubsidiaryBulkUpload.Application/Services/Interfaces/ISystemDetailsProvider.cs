namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface ISystemDetailsProvider
{
    Guid? SystemOrganisationId { get; }

    Guid? SystemUserId { get; }
}
