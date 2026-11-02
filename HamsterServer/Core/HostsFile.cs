using System.Net;

namespace HamsterServer.Core;

/// <summary>
/// Reads the Windows hosts file to tell whether a virtual host's name points at this computer —
/// the usual reason "shop.test" opens nothing in the browser although Apache serves it.
/// <para>
/// Read-only: editing the hosts file needs administrator rights, which Hamster Server does not ask
/// for. Names under <c>.localhost</c> need no entry — browsers resolve them to loopback themselves
/// (RFC 6761) — so they always count as resolvable.
/// </para>
/// </summary>
public static class HostsFile
{
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

    /// <summary>Names mapped to a loopback address (127.x.x.x or ::1). Empty when the file cannot be read.</summary>
    public static IReadOnlySet<string> ReadLoopbackNames(string? path = null)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var raw in File.ReadLines(path ?? DefaultPath))
            {
                int hash = raw.IndexOf('#');
                var line = (hash >= 0 ? raw[..hash] : raw).Trim();
                if (line.Length == 0) continue;
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !IPAddress.TryParse(parts[0], out var ip) || !IPAddress.IsLoopback(ip)) continue;
                foreach (var name in parts.Skip(1)) names.Add(name);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Unreadable hosts file: report nothing rather than a wrong "missing".
        }
        return names;
    }

    /// <summary>True when the browser will reach this computer for <paramref name="serverName"/>.</summary>
    public static bool PointsHere(string serverName, IReadOnlySet<string> loopbackNames) =>
        serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        serverName.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
        loopbackNames.Contains(serverName);
}
