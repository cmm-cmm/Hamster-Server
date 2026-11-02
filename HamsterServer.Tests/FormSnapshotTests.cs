using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer.Tests;

/// <summary>
/// Renders the configuration dialogs to PNG files for reviewing a layout change by eye. Runs only
/// when HAMSTER_FORM_SNAPSHOT_DIR names a folder; otherwise it is reported as skipped.
/// </summary>
[TestClass]
public sealed class FormSnapshotTests
{
    [TestMethod]
    public void RenderConfigurationDialogs_WhenRequested()
    {
        var target = Environment.GetEnvironmentVariable("HAMSTER_FORM_SNAPSHOT_DIR");
        if (string.IsNullOrWhiteSpace(target))
            Assert.Inconclusive("Set HAMSTER_FORM_SNAPSHOT_DIR to a folder to render the dialogs.");
        Directory.CreateDirectory(target);

        using var root = new TestDirectory();
        var www = Path.Combine(root.Path, "www");
        Directory.CreateDirectory(www);
        var phpDir = Path.Combine(root.Path, "php", "8.5.12-1");
        Directory.CreateDirectory(Path.Combine(phpDir, "ext"));
        File.WriteAllBytes(Path.Combine(phpDir, "php-cgi.exe"), []);

        var config = new AppConfig
        {
            Php = { CurrentVersion = "8.5.12-1" },
            VHosts =
            [
                new VHostEntry { ServerName = "shop.test", DocumentRoot = www, Framework = FrameworkPreset.Laravel, Enabled = true },
                new VHostEntry { ServerName = "blog.test", DocumentRoot = www, Framework = FrameworkPreset.WordPress, Enabled = false },
                new VHostEntry { ServerName = "api.test", DocumentRoot = www, Framework = FrameworkPreset.Hyperf, Enabled = true }
            ]
        };
        config.Apache.ExtraPorts = [new PortEntry { Port = 8080, Enabled = true }, new PortEntry { Port = 8443, IsSsl = true, Enabled = false }];

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using (var settings = new SettingsForm(config, root.Path, _ => { }))
                {
                    var tabs = FindControl<TabControl>(settings, "tabControl")!;
                    for (int i = 0; i < tabs.TabPages.Count; i++)
                    {
                        tabs.SelectedIndex = i;
                        Snapshot(settings, Path.Combine(target, $"settings-{i}-{tabs.TabPages[i].Name}.png"));
                    }
                }
                using (var vhosts = new VHostForm(config, root.Path))
                    Snapshot(vhosts, Path.Combine(target, "vhosts.png"));
                using (var vhost = new VHostDialog(config, root.Path, config.VHosts[0]))
                    Snapshot(vhost, Path.Combine(target, "vhost-dialog.png"));
                // HAMSTER_SNAPSHOT_ROOT = a running installation, so its httpd / php-cgi show as Hamster Server.
                using (var ports = new PortsForm(config, Environment.GetEnvironmentVariable("HAMSTER_SNAPSHOT_ROOT")))
                {
                    Snapshot(ports, Path.Combine(target, "ports.png"));
                    if (FindControl<TabControl>(ports, "tabPorts") is { } portTabs)
                    {
                        for (int i = 1; i < portTabs.TabPages.Count; i++)
                        {
                            portTabs.SelectedIndex = i;
                            Snapshot(ports, Path.Combine(target, $"ports-{i}-{portTabs.TabPages[i].Name}.png"));
                        }
                    }
                }
                using (var port = PortDialog.ForAdd(config))
                    Snapshot(port, Path.Combine(target, "port-dialog.png"));
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(60)), "Rendering timed out.");
        Assert.IsNull(failure, failure?.ToString());
    }

    private static void Snapshot(Form form, string path)
    {
        if (!form.Visible)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.ShowInTaskbar = false;
            form.Show();
        }
        for (int i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(30); }
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(path, ImageFormat.Png);
    }

    private static T? FindControl<T>(Control parent, string name) where T : Control
    {
        foreach (Control control in parent.Controls)
        {
            if (control is T match && control.Name == name) return match;
            var nested = FindControl<T>(control, name);
            if (nested is not null) return nested;
        }
        return null;
    }
}
