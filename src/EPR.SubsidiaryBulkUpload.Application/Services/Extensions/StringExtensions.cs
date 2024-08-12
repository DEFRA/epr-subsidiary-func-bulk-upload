namespace EPR.SubsidiaryBulkUpload.Application.Services.Extensions;

public static class StringExtensions
{
    public static string ToPartitionKeyFormat(this string? str)
    {
        if (str is null)
        {
            return string.Empty;
        }

        var firstDashIndex = str.IndexOf('-');

        // Find the position of the last '.'
        var dotIndex = str.LastIndexOf('.');

        // Extract the substring between the first dash and the dot
        if (firstDashIndex >= 0 && dotIndex > firstDashIndex)
        {
            str = str.Substring(firstDashIndex + 1, dotIndex - firstDashIndex - 1);
        }
        else
        {
            str = string.Empty;
        }

        return str;
    }
}
