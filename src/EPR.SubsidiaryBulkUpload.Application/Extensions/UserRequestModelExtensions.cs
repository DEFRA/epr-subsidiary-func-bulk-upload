using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class UserRequestModelExtensions
{
    public static UserRequestModel ToUserRequestModel(this IDictionary<string, string> metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var caseInsensitiveMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        if (!caseInsensitiveMetadata.ContainsKey("UserId") || !caseInsensitiveMetadata.ContainsKey("OrganisationId"))
        {
            return null;
        }

        return new UserRequestModel
        {
            UserId = caseInsensitiveMetadata["UserId"],
            OrganisationId = caseInsensitiveMetadata["OrganisationId"]
        };
    }
}
