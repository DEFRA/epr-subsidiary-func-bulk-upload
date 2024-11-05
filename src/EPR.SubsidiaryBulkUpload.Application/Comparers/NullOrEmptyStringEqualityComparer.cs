namespace EPR.SubsidiaryBulkUpload.Application.Comparers;

public class NullOrEmptyStringEqualityComparer(StringComparison stringComparisonOptions) : EqualityComparer<string>
{
    public static IEqualityComparer<string> CaseInsensitiveComparer => new NullOrEmptyStringEqualityComparer(StringComparison.OrdinalIgnoreCase);

    public override bool Equals(string? x, string? y) =>
        string.Equals(x, y, stringComparisonOptions) || (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y));

    public override int GetHashCode(string obj) =>
        string.IsNullOrEmpty(obj)
            ? 0
            : obj.GetHashCode();
}
