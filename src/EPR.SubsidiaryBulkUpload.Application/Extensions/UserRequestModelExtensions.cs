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

        // Validate and convert UserId to GUID
        if (!Guid.TryParse(caseInsensitiveMetadata["UserId"], out Guid userId))
        {
            return null;
        }

        // Validate and convert OrganisationId to GUID
        if (!Guid.TryParse(caseInsensitiveMetadata["OrganisationId"], out Guid organisationId))
        {
            return null;
        }

        return new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId
        };
    }

    public static string GenerateKey(this UserRequestModel userRequestModel, string suffix)
    {
        if (userRequestModel == null)
        {
            return null;
        }

        return $"{userRequestModel.UserId}{userRequestModel.OrganisationId}{suffix}";
    }
}
