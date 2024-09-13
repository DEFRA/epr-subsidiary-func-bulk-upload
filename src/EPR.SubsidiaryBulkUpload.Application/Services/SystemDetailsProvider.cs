using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class SystemDetailsProvider : ISystemDetailsProvider
{
    private readonly ISubsidiaryService _subsidiaryService;
    private readonly Lazy<UserOrganisation> _lazySystemUserAndOrganisation;

    public SystemDetailsProvider(ISubsidiaryService subsidiaryService)
    {
        _subsidiaryService = subsidiaryService;
        _lazySystemUserAndOrganisation = new Lazy<UserOrganisation>(GetSystemUserAndOrganisation);
    }

    public Guid SystemUserId => _systemUserAndOrganisation.UserId ?? Guid.Empty;

    public Guid SystemOrganisationId => _systemUserAndOrganisation.OrganisationId ?? Guid.Empty;

    private UserOrganisation _systemUserAndOrganisation => _lazySystemUserAndOrganisation.Value;

    private UserOrganisation GetSystemUserAndOrganisation()
    {
        var systemUserAndOrganisation = _subsidiaryService.GetSystemUserAndOrganisation().GetAwaiter().GetResult();
        return systemUserAndOrganisation;
    }
}
