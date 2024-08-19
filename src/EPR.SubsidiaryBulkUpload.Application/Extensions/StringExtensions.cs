using System.Text.RegularExpressions;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class StringExtensions
{
    public static string ToPartitionKeyFormat(this string? str)
    {
        if (str is null)
        {
            return string.Empty;
        }

        var firstDashIndex = str.IndexOf('-');

        var dotIndex = str.LastIndexOf('.');

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

        var match = Regex.Match(str, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));

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
