using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace HamsterServer.Forms;

public partial class AboutForm : Form
{
    // Kept here rather than in config: these identify the project itself, not the user's setup.
    // Mirrors the links in README.md.
    private const string RepositoryUrl = "https://github.com/cmm-cmm/Hamster-Server";
    private const string LicenseUrl = "https://github.com/cmm-cmm/Hamster-Server/blob/main/LICENSE";
    private const string SponsorUrl = "https://github.com/sponsors/cmm-cmm";

    private readonly string _systemInfo;

    public AboutForm(
        Icon appIcon,
        string apacheVersion,
        string currentPhpVersion,
        IReadOnlyList<string> installedPhpVersions,
        string rootDir)
    {
        ArgumentNullException.ThrowIfNull(appIcon);
        ArgumentNullException.ThrowIfNull(installedPhpVersions);

        InitializeComponent();

        picLogo.Image = RenderLogo(appIcon);

        var version = GetAppVersion();
        lblVersion.Text = $"Version {version}";

        lblApache.Text = Blank(apacheVersion);
        lblPhp.Text = Blank(currentPhpVersion);
        lblPhpInstalled.Text = installedPhpVersions.Count == 0
            ? "—"
            : string.Join(", ", installedPhpVersions);
        lblRuntime.Text = RuntimeInformation.FrameworkDescription;
        lblOs.Text = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        txtRoot.Text = rootDir;

        _systemInfo = BuildSystemInfo(version, apacheVersion, currentPhpVersion,
                                      installedPhpVersions, rootDir);

        // The root path is usually wider than the box; show the beginning, and put the whole
        // thing in a tooltip so it is readable without scrolling the field.
        ttRoot.SetToolTip(txtRoot, rootDir);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ActiveControl = btnClose;
        txtRoot.SelectionStart = 0;
        txtRoot.SelectionLength = 0;
    }

    /// <summary>
    /// Picks a frame out of the multi-resolution application icon instead of scaling one bitmap.
    /// The icon carries 16/32/48/64/128/256 px, so asking for twice the device size gives a frame
    /// that is downscaled rather than stretched — sharp at 100% and at 200% DPI alike.
    /// </summary>
    private Bitmap RenderLogo(Icon appIcon)
    {
        int device = LogicalToDeviceUnits(64);
        using var sized = new Icon(appIcon, device * 2, device * 2);
        return sized.ToBitmap();
    }

    private static string Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    /// <summary>
    /// Prefers the informational version (what the csproj &lt;Version&gt; produces, minus any
    /// "+&lt;sha&gt;" build suffix) and falls back to the three-part assembly version.
    /// </summary>
    private static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            int plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    private static string BuildSystemInfo(
        string version,
        string apacheVersion,
        string currentPhpVersion,
        IReadOnlyList<string> installedPhpVersions,
        string rootDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hamster Server {version}");
        sb.AppendLine($"Runtime:       {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Windows:       {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        sb.AppendLine($"Apache:        {Blank(apacheVersion)}");
        sb.AppendLine($"PHP (active):  {Blank(currentPhpVersion)}");
        sb.AppendLine($"PHP installed: {(installedPhpVersions.Count == 0 ? "—" : string.Join(", ", installedPhpVersions))}");
        sb.AppendLine($"Root:          {rootDir}");
        return sb.ToString();
    }

    private async void btnCopyInfo_Click(object sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(_systemInfo);
        }
        catch (ExternalException ex)
        {
            // Another process can hold the clipboard open; that is not worth a modal dialog.
            btnCopyInfo.Text = "Clipboard busy";
            await RestoreCopyButtonAsync();
            Debug.WriteLine($"Clipboard unavailable: {ex.Message}");
            return;
        }

        btnCopyInfo.Text = "Copied";
        await RestoreCopyButtonAsync();
    }

    // Native buttons ignore BackColor under visual styles, so confirm with the caption only.
    private async Task RestoreCopyButtonAsync()
    {
        btnCopyInfo.Enabled = false;
        await Task.Delay(1200);
        if (IsDisposed) return;
        btnCopyInfo.Text = "Copy system info";
        btnCopyInfo.Enabled = true;
    }

    private void lnkRepository_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
        OpenUrl(RepositoryUrl);

    private void lnkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
        OpenUrl(LicenseUrl);

    private void lnkSponsor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
        OpenUrl(SponsorUrl);

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // No registered browser, or the shell association is broken. Show the URL so the user
            // can still copy it by hand instead of getting a bare Win32 error.
            MessageBox.Show(this,
                $"Could not open the link:\n{ex.Message}\n\n{url}",
                "Hamster Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();
}
