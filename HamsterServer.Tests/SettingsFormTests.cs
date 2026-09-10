using System.Windows.Forms;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class SettingsFormTests
{
    [TestMethod]
    public void Constructor_PopulatesExtensionsAndTimezoneBeforeWindowHandleExists()
    {
        using var root = new TestDirectory();
        Exception? failure = null;
        var config = new AppConfig();
        config.Php.Timezone = "Pacific/Chatham";

        var thread = new Thread(() =>
        {
            try
            {
                using var form = new SettingsForm(config, root.Path);
                Assert.IsFalse(form.IsHandleCreated);
                var timezone = FindControl<ComboBox>(form, "cboTimezone");
                Assert.IsNotNull(timezone);
                Assert.AreEqual(ComboBoxStyle.DropDownList, timezone.DropDownStyle);
                Assert.AreEqual("Pacific/Chatham", timezone.SelectedItem);
                CollectionAssert.Contains(timezone.Items, "UTC");
                CollectionAssert.Contains(timezone.Items, "Asia/Ho_Chi_Minh");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "Settings form construction timed out.");
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
