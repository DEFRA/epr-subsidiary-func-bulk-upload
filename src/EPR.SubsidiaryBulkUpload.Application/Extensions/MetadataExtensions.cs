using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class MetadataExtensions
{
    public static UserRequestModel ToUserRequestModel(this IDictionary<string, string> metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var caseInsensitiveMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        if (!caseInsensitiveMetadata.TryGetValue("UserId", out var userIdString) || !caseInsensitiveMetadata.TryGetValue("OrganisationId", out var organisationIdString))
        {
            return null;
        }

        if (!Guid.TryParse(userIdString, out Guid userId))
        {
            return null;
        }

        if (!Guid.TryParse(organisationIdString, out Guid organisationId))
        {
            return null;
        }

        Guid complianceSchemeId = Guid.NewGuid();
        if (caseInsensitiveMetadata.TryGetValue("ComplianceSchemeId", out var complianceSchemeIdString))
        {
            Guid.TryParse(complianceSchemeIdString, out complianceSchemeId);
        }

        return new UserRequestModel
        {
            UserId = userId,
            OrganisationId = organisationId,
            ComplianceSchemeId = complianceSchemeId
        };
    }

    public static string GetFileName(this IDictionary<string, string> metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var caseInsensitiveMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        if (!caseInsensitiveMetadata.TryGetValue("FileName", out var fileName))
        {
            return null;
        }

        return fileName;
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
