namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

public static class JsonExtensions
{
    public static StringContent ToJsonContent<T>(this T parameters)
    {
        var jsonContent = JsonConvert.SerializeObject(parameters);
        return new StringContent(jsonContent, Encoding.UTF8, "application/json");
    }

    public static string GetStringFromJsonElement(
        this JsonElement element,
        string propertyName,
        string defaultValue = default)
    {
        return element.ValueKind != JsonValueKind.Undefined
                     && element.TryGetProperty(propertyName, out var property)
                     && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : defaultValue;
    }
}