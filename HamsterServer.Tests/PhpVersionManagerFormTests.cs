using System.Windows.Forms;
using HamsterServer.Core;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class PhpVersionManagerFormTests
{
    [TestMethod]
    public void Constructor_ListsInstalledVersionsBeforeCatalogIsLoaded()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "8.4.1");
        CreatePhp(root.Path, "8.5.1");
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var manager = new PhpVersionManager(root.Path);
                using var form = new PhpVersionManagerForm(manager, new AppConfig { Php = { CurrentVersion = "8.4.1" } });
                Assert.IsFalse(form.IsHandleCreated);
                var versions = FindControl<ListView>(form, "lvVersions");
                Assert.IsNotNull(versions);
                Assert.AreEqual(2, versions.Items.Count);
                Assert.AreEqual("8.5.1", versions.Items[0].Text);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)),
            "PHP version manager construction timed out.");
        Assert.IsNull(failure, failure?.ToString());
    }

    // The case the redesign exists for: sqlsrv was added to 8.5.1 by hand, 8.5.2 lacks it, and the
    // window has to say so — and offer the copy — without the user comparing ext\ folders.
    [TestMethod]
    public void Constructor_ShowsMissingAndCopyableExtensionsForTheActiveRuntime()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "8.5.1");
        CreatePhp(root.Path, "8.5.2");
        CreatePhp(root.Path, "8.4.9");
        File.WriteAllBytes(Path.Combine(root.Path, "php", "8.5.1", "ext", "php_sqlsrv.dll"), []);
        var config = new AppConfig { Php = { CurrentVersion = "8.5.2", EnabledExtensions = ["sqlsrv"] } };
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var manager = new PhpVersionManager(root.Path);
                using var form = new PhpVersionManagerForm(manager, config);
                var versions = FindControl<ListView>(form, "lvVersions")!;
                var active = versions.Items.Cast<ListViewItem>().Single(i => i.Text == "8.5.2");
                StringAssert.StartsWith(active.SubItems[1].Text, "Active");
                StringAssert.Contains(active.SubItems[2].Text, "1 missing");
                StringAssert.Contains(active.SubItems[2].Text, "1 can be copied");
                Assert.AreEqual("installed", active.Group?.Name);
                StringAssert.StartsWith(versions.Items.Cast<ListViewItem>().Single(i => i.Text == "8.5.1").SubItems[1].Text,
                    "Older patch");

                var title = FindControl<Label>(form, "lblActiveTitle")!;
                Assert.AreEqual("PHP 8.5.2 is active", title.Text);
                var details = FindControl<Label>(form, "lblDetails")!;
                StringAssert.Contains(details.Text, "php_sqlsrv.dll", "The active row is selected and its details name the file.");

                WriteSnapshotWhenRequested(form);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "PHP version manager construction timed out.");
        Assert.IsNull(failure, failure?.ToString());
    }

    /// <summary>Renders the window to HAMSTER_FORM_SNAPSHOT (a .png path) for reviewing the layout by eye.</summary>
    private static void WriteSnapshotWhenRequested(Form form)
    {
        var target = Environment.GetEnvironmentVariable("HAMSTER_FORM_SNAPSHOT");
        if (string.IsNullOrWhiteSpace(target)) return;

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new System.Drawing.Point(-20000, -20000);
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(target, System.Drawing.Imaging.ImageFormat.Png);
        form.Hide();
    }

    private static void CreatePhp(string root, string version)
    {
        string directory = Path.Combine(root, "php", version);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "php-cgi.exe"), []);
        Directory.CreateDirectory(Path.Combine(directory, "ext"));
        File.WriteAllBytes(Path.Combine(directory, "ext", "php_curl.dll"), []);
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
