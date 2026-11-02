using System.Windows.Forms;
using HamsterServer.Core;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class SettingsFormTests
{
    // php.ini directives and extensions moved to PhpConfigForm so each setting has one editor.
    // A control reintroduced here would let the two dialogs write different values again.
    [TestMethod]
    public void Constructor_LeavesPhpIniDirectivesToPhpConfiguration()
    {
        using var root = new TestDirectory();
        RunOnStaThread(() =>
        {
            using var form = new SettingsForm(new AppConfig(), root.Path);
            Assert.IsFalse(form.IsHandleCreated);
            foreach (var name in new[] { "cboTimezone", "clbExtensions", "nudMemoryLimit", "nudMaxExecTime" })
                Assert.IsNull(FindControl<Control>(form, name), $"{name} belongs to PhpConfigForm.");

            // Without a host callback there is nothing to open, so the pointer button is hidden.
            var open = FindControl<Button>(form, "btnOpenPhpConfig");
            Assert.IsNotNull(open);
            Assert.IsFalse(open.Visible);
        });
    }

    [TestMethod]
    public void PhpConfigForm_PopulatesTimezoneAndExtensionsBeforeWindowHandleExists()
    {
        using var root = new TestDirectory();
        var phpDir = Path.Combine(root.Path, "php", "8.4.1");
        var extDir = Path.Combine(phpDir, "ext");
        Directory.CreateDirectory(extDir);
        foreach (var dll in new[] { "php_curl.dll", "php_intl.dll", "php_opcache.dll" })
            File.WriteAllBytes(Path.Combine(extDir, dll), []);
        File.WriteAllBytes(Path.Combine(phpDir, "php-cgi.exe"), []);

        var config = new AppConfig();
        config.Php.Timezone = "Pacific/Chatham";
        // "imap" has no DLL in this runtime: it must survive a save made from this dialog.
        config.Php.EnabledExtensions = ["curl", "imap"];

        RunOnStaThread(() =>
        {
            var manager = new PhpVersionManager(root.Path);
            var php = manager.Scan().Single();
            using var form = new PhpConfigForm(config, php, _ => Task.CompletedTask);
            Assert.IsFalse(form.IsHandleCreated);

            var timezone = FindControl<ComboBox>(form, "cboTimezone");
            Assert.IsNotNull(timezone);
            Assert.AreEqual("Pacific/Chatham", timezone.Text);
            CollectionAssert.Contains(timezone.Items, "UTC");
            CollectionAssert.Contains(timezone.Items, TimezoneCatalog.Default);

            var extensions = FindControl<CheckedListBox>(form, "clbExtensions");
            Assert.IsNotNull(extensions);
            CollectionAssert.AreEqual(new[] { "curl", "intl" }, extensions.Items.Cast<string>().ToArray(),
                "OPcache is a zend_extension and must not be offered as a normal extension.");
            CollectionAssert.AreEqual(new[] { "curl" }, extensions.CheckedItems.Cast<string>().ToArray());
        });
    }

    private static void RunOnStaThread(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "Form construction timed out.");
        Assert.IsNull(failure, failure?.ToString());
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
