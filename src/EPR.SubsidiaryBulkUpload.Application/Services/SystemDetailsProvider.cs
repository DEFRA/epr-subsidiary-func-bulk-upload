namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class SystemDetailsProvider(
    ISubsidiaryService subsidiaryService)
    : ISystemDetailsProvider
{
    private readonly ISubsidiaryService _subsidiaryService = subsidiaryService;

    private Guid? _systemUserId;
    private Guid? _systemOrganisationId;

    public Guid? SystemUserId
    {
        get
        {
            if (_systemUserId is null)
            {
                GetSystemUserAndOrganisation().GetAwaiter().GetResult();
            }

            return _systemUserId;
        }
    }

    public Guid? SystemOrganisationId
    {
        get
        {
            if (_systemOrganisationId is null)
            {
                GetSystemUserAndOrganisation().GetAwaiter().GetResult();
            }

            return _systemOrganisationId;
        }
    }

    public async Task GetSystemUserAndOrganisation()
    {
        var result = await _subsidiaryService.GetSystemUserAndOrganisation();

        _systemOrganisationId = result.OrganisationId;
        _systemUserId = result.UserId;
    }
}
