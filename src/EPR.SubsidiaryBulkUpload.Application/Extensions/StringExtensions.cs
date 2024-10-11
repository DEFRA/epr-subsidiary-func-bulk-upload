using System.Text.RegularExpressions;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class StringExtensions
{
    public static string ToPartitionKey(this string? str)
    {
        if (str is null)
        {
            return string.Empty;
        }

        const string pattern = @"\d{4}-\d{2}-\d{2}";
        var match = Regex.Match(str, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));

        if (match.Success)
        {
            return match.Value;
        }

        return string.Empty;
    }

    public static (int PartNumber, int TotalFiles) ToFilePartNumberAndCount(this string? str)
    {
        if (str is null)
        {
            return (0, 0);
        }

        const string pattern = "part(?<Part>\\d+)_(?<Total>\\d+)";
        var matches = Regex.Match(str, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));

        if (matches.Success &&
            int.TryParse(matches.Groups["Part"].Value, out var part) &&
            int.TryParse(matches.Groups["Total"].Value, out var total))
        {
            return (part, total);
        }

        return (0, 0);
    }
}
