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
