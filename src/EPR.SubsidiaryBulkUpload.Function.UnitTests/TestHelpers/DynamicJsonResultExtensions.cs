using Microsoft.AspNetCore.Mvc;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests.TestHelpers;

public static class DynamicJsonResultExtensions
{
    public static T GetDynamicPropertyValue<T>(this JsonResult jsonResult, string propertyName)
    {
        var property = jsonResult.Value?.GetType().GetProperties()
                .Where(p => string.Compare(p.Name, propertyName) == 0)
                .FirstOrDefault()
                ?? throw new ArgumentException("propertyName not found", propertyName);

        return (T)property.GetValue(jsonResult.Value, null);
    }
}
