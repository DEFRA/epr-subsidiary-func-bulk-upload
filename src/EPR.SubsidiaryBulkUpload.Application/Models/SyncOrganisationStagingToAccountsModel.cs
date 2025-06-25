namespace EPR.SubsidiaryBulkUpload.Application.Models;

public class SyncOrganisationStagingToAccountsModel
{
    public int NumErroredRecords { get; set; }

    public int NumProcessedRecords { get; set; }
}