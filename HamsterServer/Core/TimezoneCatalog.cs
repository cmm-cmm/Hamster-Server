namespace HamsterServer.Core;

/// <summary>
/// IANA identifiers for PHP's <c>date.timezone</c>, built from the zones Windows knows about.
/// </summary>
public static class TimezoneCatalog
{
    public const string Default = "Asia/Ho_Chi_Minh";

    /// <summary>
    /// The same character rule <see cref="ConfigManager"/> normalizes with. A name that fails it is
    /// reset to <see cref="Default"/> on save, so a dialog should refuse it visibly instead.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Trim().All(ch => char.IsLetterOrDigit(ch) || ch is '/' or '_' or '-' or '+');

    /// <summary>
    /// UTC and <see cref="Default"/> first, then every other zone alphabetically. The configured
    /// value is always included, so a zone Windows does not list is never silently dropped.
    /// </summary>
    public static IReadOnlyList<string> Build(string? configured)
    {
        var zones = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var zone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (zone.Id.Contains('/'))
                    zones.Add(zone.Id);
                else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId))
                    zones.Add(ianaId);
            }
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // A missing or corrupt OS timezone registry leaves only the safe defaults below.
        }

        if (IsValid(configured)) zones.Add(configured!.Trim());

        var result = new List<string> { "UTC", Default };
        result.AddRange(zones.Where(zone =>
            !zone.Equals("UTC", StringComparison.OrdinalIgnoreCase) &&
            !zone.Equals(Default, StringComparison.OrdinalIgnoreCase)));
        return result;
    }
}
