using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public interface ICompaniesHouseDataProvider
{
    /// <summary>
    /// Set the companies house data in the subsidiary organisation model from the local store or through API.
    /// </summary>
    /// <param name="subsidiaryModel">The model to populate.</param>
    /// <returns>true is the organisation model was updated; false Otherwise.</returns>
    Task<bool> SetCompaniesHouseData(OrganisationModel subsidiaryModel);
}