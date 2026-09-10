using System.Windows.Forms;
using HamsterServer.Core;
using HamsterServer.Forms;

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
                using var form = new PhpVersionManagerForm(manager, "8.4.1");
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

    private static void CreatePhp(string root, string version)
    {
        string directory = Path.Combine(root, "php", version);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "php-cgi.exe"), []);
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
