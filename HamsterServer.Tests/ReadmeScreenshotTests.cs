using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using HamsterServer.Core;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer.Tests;

/// <summary>
/// Renders the screenshots in site/images/ used by the README and the website, from sample data only: a fake php.net
/// catalog, sample listeners for the Ports window and sample sites — so no path, process or address
/// of the machine running it ends up in the repository. Runs only when HAMSTER_README_SCREENSHOTS
/// names the output folder.
/// </summary>
[TestClass]
public sealed class ReadmeScreenshotTests
{
    private const string SampleRoot = @"C:\HamsterServer";

    [TestMethod]
    public void RenderReadmeScreenshots_WhenRequested()
    {
        var target = Environment.GetEnvironmentVariable("HAMSTER_README_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(target))
            Assert.Inconclusive("Set HAMSTER_README_SCREENSHOTS to a folder to render the README screenshots.");
        Directory.CreateDirectory(target);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                RenderPhpVersionManager(Path.Combine(target, "php-version-manager.png"));
                RenderPorts(target);
                RenderVirtualHosts(Path.Combine(target, "virtual-hosts.png"));
                RenderSettings(Path.Combine(target, "settings-php.png"));
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(90)), "Rendering timed out.");
        Assert.IsNull(failure, failure?.ToString());
    }

    // ─── PHP Version Manager: 8.5.9 active, 8.5.12-1 available ──────────────────

    private static void RenderPhpVersionManager(string path)
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.9", "php_curl.dll", "php_mbstring.dll", "php_intl.dll", "php_sqlsrv.dll", "php_pdo_sqlsrv.dll");
        CreateRuntime(root.Path, "8.4.24", "php_curl.dll", "php_mbstring.dll", "php_intl.dll");
        var config = new AppConfig
        {
            Php = { CurrentVersion = "8.5.9", EnabledExtensions = ["curl", "intl", "mbstring", "pdo_sqlsrv", "sqlsrv"] }
        };
        var manager = new PhpVersionManager(root.Path, new HttpClient(new CatalogHandler()));
        using var form = new PhpVersionManagerForm(manager, config, switchAsync: _ => Task.CompletedTask);

        Show(form);
        var list = Find<ListView>(form, "lvVersions");
        WaitUntil(() => list.Items.Cast<ListViewItem>().Any(i => i.Text == "8.5.12-1"));
        list.Items.Cast<ListViewItem>().Single(i => i.Text == "8.5.12-1").Selected = true;
        Save(form, path);
    }

    // ─── Ports: Hamster Server on 80 / 9000, Node holding the extra port 8080 ───

    private static void RenderPorts(string target)
    {
        var config = new AppConfig();
        config.Apache.Port = 80;
        config.Apache.SslPort = 443;
        config.Apache.ExtraPorts = [new PortEntry { Port = 8080, Enabled = true }, new PortEntry { Port = 8443, IsSsl = true, Enabled = false }];
        config.Php.FcgiPort = 9000;

        using var form = new PortsForm(config, SampleRoot) { Scanner = SampleListeners };
        Show(form);
        WaitUntil(() => Find<ListView>(form, "lvSystem").Items.Count > 0);
        Save(form, Path.Combine(target, "ports-apache.png"));

        Find<TabControl>(form, "tabPorts").SelectedIndex = 1;
        Save(form, Path.Combine(target, "ports-system.png"));
    }

    private static IReadOnlyList<ListeningPort> SampleListeners()
    {
        ListeningPort L(int port, int pid, string name, string path, params string[] addresses) =>
            new(port, addresses, pid, name, path);
        const string Loop = "127.0.0.1";
        return
        [
            L(80, 6120, "httpd", @"c:\hamsterserver\apache\apache24\bin\httpd.exe", Loop),
            L(135, 1180, "svchost", @"c:\windows\system32\svchost.exe", "0.0.0.0", "::"),
            L(445, 4, "System", null!, "0.0.0.0", "::"),
            L(1433, 4312, "sqlservr", @"c:\program files\microsoft sql server\mssql16.sqlexpress\mssql\binn\sqlservr.exe", "0.0.0.0", "::"),
            L(3306, 3980, "mysqld", @"c:\xampp\mysql\bin\mysqld.exe", "0.0.0.0", "::"),
            L(5173, 9244, "node", @"c:\program files\nodejs\node.exe", Loop),
            L(6379, 2716, "redis-server", @"c:\program files\redis\redis-server.exe", Loop),
            L(8080, 8836, "node", @"c:\program files\nodejs\node.exe", "0.0.0.0", "::"),
            L(9000, 6388, "php-cgi", @"c:\hamsterserver\php\8.5.12-1\php-cgi.exe", Loop),
            L(9003, 7020, "Code", @"c:\program files\microsoft vs code\code.exe", Loop),
            L(27017, 3364, "mongod", @"c:\program files\mongodb\server\8.0\bin\mongod.exe", Loop),
            L(49664, 1004, "lsass", @"c:\windows\system32\lsass.exe", "0.0.0.0", "::")
        ];
    }

    // ─── Virtual Hosts ───────────────────────────────────────────────────────────

    private static void RenderVirtualHosts(string path)
    {
        var config = new AppConfig
        {
            VHosts =
            [
                new VHostEntry { ServerName = "shop.test", ServerAlias = "www.shop.test", DocumentRoot = @"D:\Sites\shop", Framework = FrameworkPreset.Laravel },
                new VHostEntry { ServerName = "blog.localhost", DocumentRoot = @"D:\Sites\blog", Framework = FrameworkPreset.WordPress },
                new VHostEntry { ServerName = "api.localhost", DocumentRoot = @"D:\Sites\api", Framework = FrameworkPreset.Symfony },
                new VHostEntry { ServerName = "legacy.test", DocumentRoot = @"D:\Sites\legacy", Framework = FrameworkPreset.CodeIgniter, Enabled = false }
            ]
        };
        using var form = new VHostForm(config, SampleRoot);
        Show(form);
        Find<ListView>(form, "lvVHosts").Items[0].Selected = true;
        Save(form, path);
    }

    // ─── Settings › PHP ──────────────────────────────────────────────────────────

    private static void RenderSettings(string path)
    {
        using var root = new TestDirectory();
        using var form = new SettingsForm(new AppConfig(), root.Path, _ => { });
        Show(form);
        Find<TabControl>(form, "tabControl").SelectedIndex = 1;
        Save(form, path);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static void CreateRuntime(string root, string version, params string[] extensions)
    {
        var dir = Path.Combine(root, "php", version);
        Directory.CreateDirectory(Path.Combine(dir, "ext"));
        File.WriteAllBytes(Path.Combine(dir, "php-cgi.exe"), []);
        foreach (var ext in extensions) File.WriteAllBytes(Path.Combine(dir, "ext", ext), []);
    }

    private static void Show(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.ShowInTaskbar = false;
        form.Show();
        Pump(300);
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.IsTrue(DateTime.UtcNow < deadline, "The window never reached the state to capture.");
            Pump(50);
        }
        Pump(200);
    }

    private static void Pump(int milliseconds)
    {
        var until = DateTime.UtcNow.AddMilliseconds(milliseconds);
        do
        {
            Application.DoEvents();
            Thread.Sleep(15);
        } while (DateTime.UtcNow < until);
    }

    private static void Save(Form form, string path)
    {
        Pump(200);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(path, ImageFormat.Png);
    }

    private static T Find<T>(Control parent, string name) where T : Control =>
        FindOrNull<T>(parent, name) ?? throw new InvalidOperationException($"{name} not found.");

    private static T? FindOrNull<T>(Control parent, string name) where T : Control
    {
        foreach (Control control in parent.Controls)
        {
            if (control is T match && control.Name == name) return match;
            var nested = FindOrNull<T>(control, name);
            if (nested is not null) return nested;
        }
        return null;
    }

    /// <summary>A php.net look-alike: the October 2026 catalog shape for four branches.</summary>
    private sealed class CatalogHandler : HttpMessageHandler
    {
        private static readonly byte[] Catalog = BuildCatalog();

        private static byte[] BuildCatalog()
        {
            var json = new StringBuilder("{");
            var entries = new[] { ("8.5", "8.5.12-1", "vs17", "34.6MB"), ("8.4", "8.4.27-1", "vs17", "33.1MB"),
                                  ("8.3", "8.3.36-1", "vs16", "31.0MB"), ("8.2", "8.2.35-1", "vs16", "30.2MB") };
            foreach (var (branch, version, vs, size) in entries)
            {
                string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(version))).ToLowerInvariant();
                json.Append($$"""
                    "{{branch}}": { "version": "{{version}}", "nts-{{vs}}-x64": { "mtime": "2026-10-26T13:05:05+00:00",
                      "zip": { "path": "php-{{version}}-nts-Win32-{{vs}}-x64.zip", "size": "{{size}}", "sha256": "{{hash}}" } } },
                    """);
            }
            return Encoding.UTF8.GetBytes(json.ToString().TrimEnd().TrimEnd(',') + "}");
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new ByteArrayContent(Catalog) });
    }
}
