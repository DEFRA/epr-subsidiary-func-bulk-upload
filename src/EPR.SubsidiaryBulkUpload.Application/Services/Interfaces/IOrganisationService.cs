using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface IOrganisationService
{
    Task<SyncOrganisationStagingToAccountsModel?> SyncStagingToAccounts();
}