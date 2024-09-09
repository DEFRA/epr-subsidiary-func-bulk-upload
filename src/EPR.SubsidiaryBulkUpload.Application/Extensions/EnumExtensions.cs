using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())[0]
            .GetCustomAttribute<DisplayAttribute>()
            ?.GetName();
    }
}