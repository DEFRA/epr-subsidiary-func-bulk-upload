using System.Text.RegularExpressions;

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

    public static string ToFindPartitionKey(this string? str)
    {
        if (str is null)
        {
            return string.Empty;
        }

        string pattern = @"\d{4}-\d{2}-\d{2}";

        // Find match using Regex.Match
        Match match = Regex.Match(str, pattern);

        if (match.Success)
        {
            return match.Value;
        }
        else
        {
            return string.Empty;
        }
    }
}
