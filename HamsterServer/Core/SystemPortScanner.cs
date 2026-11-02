using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using HamsterServer.Models;

namespace HamsterServer.Core;

/// <summary>Who a listening port belongs to, from Hamster Server's point of view.</summary>
public enum PortOwnerKind
{
    /// <summary>A process that has nothing to do with Hamster Server or its configured ports.</summary>
    Other,
    /// <summary>This installation's httpd.exe.</summary>
    HamsterApache,
    /// <summary>This installation's php-cgi.exe (the FastCGI port).</summary>
    HamsterPhp,
    /// <summary>Another process holds a port Hamster Server is configured to use: Apache/PHP cannot start.</summary>
    Conflict
}

/// <summary>One TCP port in the LISTEN state. IPv4 and IPv6 sockets of the same process are merged.</summary>
public sealed record ListeningPort(
    int Port,
    IReadOnlyList<string> Addresses,
    int ProcessId,
    string ProcessName,
    string? ProcessPath)
{
    /// <summary>True when every address is loopback: only this computer can connect.</summary>
    public bool LocalOnly => Addresses.All(a => a.StartsWith("127.", StringComparison.Ordinal) || a == "::1");
}

/// <summary>A listening port with its meaning for Hamster Server attached.</summary>
public sealed record PortUsage(ListeningPort Listener, PortOwnerKind Owner, string? Role, string? WellKnownService);

/// <summary>
/// Lists the machine's listening TCP ports with their owning process — the "System ports" view of
/// the Ports window — and says which belong to Hamster Server and which block it.
/// <para>
/// Uses <c>GetExtendedTcpTable</c> (the API netstat -ano is built on) instead of parsing netstat's
/// localised text output, and <see cref="ServerManager.TryGetProcessImagePath"/> for the owner, which
/// works on most processes without elevation. System processes whose path cannot be read keep their
/// name and PID.
/// </para>
/// </summary>
public static class SystemPortScanner
{
    public static IReadOnlyList<ListeningPort> GetTcpListeners()
    {
        var raw = new List<(int Port, string Address, int Pid)>();
        raw.AddRange(ReadTable(AfInet));
        raw.AddRange(ReadTable(AfInet6));

        var names = new Dictionary<int, (string Name, string? Path)>();
        (string Name, string? Path) Owner(int pid)
        {
            if (names.TryGetValue(pid, out var known)) return known;
            string name = pid switch { 0 => "System Idle Process", 4 => "System", _ => "" };
            string? path = pid > 4 ? ServerManager.TryGetProcessImagePath(pid) : null;
            if (name.Length == 0)
            {
                try
                {
                    using var process = Process.GetProcessById(pid);
                    name = process.ProcessName;
                }
                catch
                {
                    name = path is null ? $"PID {pid}" : Path.GetFileNameWithoutExtension(path);
                }
            }
            return names[pid] = (name, path);
        }

        return raw
            .GroupBy(r => (r.Port, r.Pid))
            .Select(g =>
            {
                var (name, path) = Owner(g.Key.Pid);
                var addresses = g.Select(r => r.Address).Distinct().OrderBy(a => a.Contains(':') ? 1 : 0).ThenBy(a => a).ToList();
                return new ListeningPort(g.Key.Port, addresses, g.Key.Pid, name, path);
            })
            .OrderBy(p => p.Port)
            .ThenBy(p => p.ProcessId)
            .ToList();
    }

    /// <summary>
    /// Attaches Hamster Server's view to each listener: its own httpd / php-cgi (matched by image path
    /// under <paramref name="rootDir"/>, the same test the kill sweeps use), a configured port held by
    /// something else, or an unrelated process — plus the role the port plays in the configuration.
    /// </summary>
    public static IReadOnlyList<PortUsage> Classify(IEnumerable<ListeningPort> listeners, AppConfig config, string? rootDir)
    {
        var roles = ConfiguredRoles(config);
        string? root = string.IsNullOrWhiteSpace(rootDir)
            ? null
            : Path.GetFullPath(rootDir).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant() + Path.DirectorySeparatorChar;

        return listeners.Select(listener =>
        {
            roles.TryGetValue(listener.Port, out var role);
            bool ours = root is not null && listener.ProcessPath is { } path &&
                        path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            var kind = ours && listener.ProcessName.Equals("httpd", StringComparison.OrdinalIgnoreCase)
                ? PortOwnerKind.HamsterApache
                : ours && listener.ProcessName.Equals("php-cgi", StringComparison.OrdinalIgnoreCase)
                    ? PortOwnerKind.HamsterPhp
                    : role is not null ? PortOwnerKind.Conflict : PortOwnerKind.Other;
            return new PortUsage(listener, kind, role, WellKnown(listener.Port));
        }).ToList();
    }

    /// <summary>Every port the configuration makes Apache or PHP listen on, with what it is for.</summary>
    public static Dictionary<int, string> ConfiguredRoles(AppConfig config)
    {
        var roles = new Dictionary<int, string> { [config.Apache.Port] = "Apache HTTP (primary)" };
        if (config.Apache.EnableSsl) roles.TryAdd(config.Apache.SslPort, "Apache HTTPS (primary)");
        foreach (var extra in (config.Apache.ExtraPorts ?? []).Where(p => p.Enabled))
            roles.TryAdd(extra.Port, extra.IsSsl ? "Apache HTTPS (extra)" : "Apache HTTP (extra)");
        roles.TryAdd(config.Php.FcgiPort, "PHP FastCGI");
        return roles;
    }

    /// <summary>What a developer usually finds on a port, so an unfamiliar number means something.</summary>
    public static string? WellKnown(int port) => port switch
    {
        21 => "FTP",
        22 => "SSH",
        25 => "SMTP",
        53 => "DNS",
        80 => "HTTP",
        135 => "Windows RPC",
        139 or 445 => "Windows file sharing",
        443 => "HTTPS",
        1025 or 1080 => "Mail catcher",
        1433 => "SQL Server",
        1521 => "Oracle",
        2375 or 2376 => "Docker",
        3000 => "Node / React dev server",
        3306 => "MySQL / MariaDB",
        3389 => "Remote Desktop",
        4200 => "Angular dev server",
        5000 => "Flask / .NET dev server",
        5173 => "Vite dev server",
        5432 => "PostgreSQL",
        5672 => "RabbitMQ",
        6379 => "Redis",
        8000 => "HTTP (dev)",
        8025 => "Mailpit / MailHog UI",
        8080 or 8081 or 8888 => "HTTP (alternate)",
        8443 => "HTTPS (alternate)",
        9000 => "PHP FastCGI / FPM",
        9003 => "Xdebug",
        9200 => "Elasticsearch",
        9501 => "Swoole / Hyperf",
        11211 => "Memcached",
        27017 => "MongoDB",
        _ => null
    };

    // ─── iphlpapi ─────────────────────────────────────────────────────────────

    private const int AfInet = 2;
    private const int AfInet6 = 23;
    private const int TcpTableOwnerPidListener = 3;
    private const int ErrorInsufficientBuffer = 122;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr table, ref int size, bool sorted, int addressFamily, int tableClass, uint reserved);

    private static List<(int Port, string Address, int Pid)> ReadTable(int family)
    {
        var rows = new List<(int, string, int)>();
        int size = 0;
        uint result = GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, TcpTableOwnerPidListener, 0);
        if (result != ErrorInsufficientBuffer && result != 0) return rows;

        // The table can grow between the sizing call and the real one; retry a few times.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                result = GetExtendedTcpTable(buffer, ref size, false, family, TcpTableOwnerPidListener, 0);
                if (result == ErrorInsufficientBuffer) continue;
                if (result != 0) return rows;

                int count = Marshal.ReadInt32(buffer);
                IntPtr row = buffer + 4;
                for (int i = 0; i < count; i++)
                {
                    if (family == AfInet)
                    {
                        // MIB_TCPROW_OWNER_PID: state, localAddr, localPort, remoteAddr, remotePort, pid
                        uint address = (uint)Marshal.ReadInt32(row, 4);
                        int port = NetworkPort(Marshal.ReadInt32(row, 8));
                        int pid = Marshal.ReadInt32(row, 20);
                        rows.Add((port, new IPAddress(address).ToString(), pid));
                        row += 24;
                    }
                    else
                    {
                        // MIB_TCP6ROW_OWNER_PID: localAddr[16], scope, localPort, remoteAddr[16],
                        // remoteScope, remotePort, state, pid
                        var bytes = new byte[16];
                        Marshal.Copy(row, bytes, 0, 16);
                        int port = NetworkPort(Marshal.ReadInt32(row, 20));
                        int pid = Marshal.ReadInt32(row, 52);
                        rows.Add((port, new IPAddress(bytes).ToString(), pid));
                        row += 56;
                    }
                }
                return rows;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        return rows;
    }

    /// <summary>The table stores the port in network byte order in the low 16 bits of a DWORD.</summary>
    internal static int NetworkPort(int raw) => ((raw & 0xFF) << 8) | ((raw >> 8) & 0xFF);
}
