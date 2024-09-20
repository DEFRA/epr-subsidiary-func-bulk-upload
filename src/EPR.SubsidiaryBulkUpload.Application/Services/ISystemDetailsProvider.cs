namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface ISystemDetailsProvider
{
    Guid? SystemOrganisationId { get; }

    Guid? SystemUserId { get; }
}
