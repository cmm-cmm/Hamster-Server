using System.Globalization;
using System.Text.RegularExpressions;

namespace HamsterServer.Core;

/// <summary>
/// A PHP for Windows version as windows.php.net publishes it: <c>major.minor.patch</c>, optionally a
/// fourth component, optionally a <c>-N</c> Windows rebuild suffix (<c>8.5.12-1</c>).
/// <para>
/// The suffix is how php.net re-releases the same PHP version with rebuilt binaries (newer OpenSSL,
/// curl, a build fix). From October 2026 every supported branch in releases.json carries one, and
/// <see cref="Version.TryParse(string?, out Version?)"/> rejects it — which made the release catalog
/// drop 8.2 to 8.5 entirely, so an install on 8.5.9 never saw 8.5.12. Compare with this type, never
/// with <see cref="Version"/>: <c>8.5.12-2</c> is newer than <c>8.5.12-1</c>, which is newer than
/// <c>8.5.12</c> and <c>8.5.9</c>.
/// </para>
/// </summary>
public readonly partial record struct PhpVersionKey(Version Version, int Build) : IComparable<PhpVersionKey>
{
    public static PhpVersionKey Zero { get; } = new(new Version(0, 0), 0);

    /// <summary>"8.5" — the branch that shares an extension ABI.</summary>
    public string Branch => $"{Version.Major}.{Version.Minor}";

    public static bool TryParse(string? text, out PhpVersionKey key)
    {
        key = Zero;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var match = Pattern().Match(text);
        if (!match.Success || !Version.TryParse(match.Groups["version"].Value, out var version)) return false;
        int build = match.Groups["build"].Success
            ? int.Parse(match.Groups["build"].Value, CultureInfo.InvariantCulture)
            : 0;
        key = new PhpVersionKey(version, build);
        return true;
    }

    /// <summary>Unparseable text sorts below every real version.</summary>
    public static PhpVersionKey ParseOrZero(string? text) => TryParse(text, out var key) ? key : Zero;

    public static bool IsValid(string? text) => TryParse(text, out _);

    public bool IsSameBranch(PhpVersionKey other) =>
        Version.Major == other.Version.Major && Version.Minor == other.Version.Minor;

    public int CompareTo(PhpVersionKey other)
    {
        int byVersion = Version.CompareTo(other.Version);
        return byVersion != 0 ? byVersion : Build.CompareTo(other.Build);
    }

    public static bool operator >(PhpVersionKey a, PhpVersionKey b) => a.CompareTo(b) > 0;
    public static bool operator <(PhpVersionKey a, PhpVersionKey b) => a.CompareTo(b) < 0;
    public static bool operator >=(PhpVersionKey a, PhpVersionKey b) => a.CompareTo(b) >= 0;
    public static bool operator <=(PhpVersionKey a, PhpVersionKey b) => a.CompareTo(b) <= 0;

    public override string ToString() => Build > 0 ? $"{Version}-{Build}" : Version.ToString();

    // Bounded lengths keep int.Parse and Version.TryParse from overflowing on hostile catalog data.
    [GeneratedRegex(@"^(?<version>\d{1,4}\.\d{1,4}\.\d{1,6}(?:\.\d{1,6})?)(?:-(?<build>\d{1,4}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
