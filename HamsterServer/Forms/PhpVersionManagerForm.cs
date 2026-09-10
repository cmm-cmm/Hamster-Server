using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class PhpVersionManagerForm : Form
{
    private readonly PhpVersionManager _manager;
    private readonly string _currentVersion;
    private IReadOnlyList<PhpReleaseInfo> _releases = [];
    private CancellationTokenSource? _operationCancellation;
    private bool _catalogLoaded;

    public bool VersionsChanged { get; private set; }
    public bool VersionInstalled { get; private set; }

    public PhpVersionManagerForm(PhpVersionManager manager, string currentVersion)
    {
        InitializeComponent();
        _manager = manager;
        _currentVersion = currentVersion;
        InitIcons();
        RebuildList();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshCatalogAsync();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _operationCancellation is null)
        {
            Close();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_operationCancellation is not null)
        {
            _operationCancellation.Cancel();
            e.Cancel = true;
        }
        base.OnFormClosing(e);
    }

    private void InitIcons()
    {
        btnRefresh.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Refresh, UiTheme.AccentAction);
        btnInstall.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Add, UiTheme.StatusRunning);
        btnUpdate.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Refresh, UiTheme.AccentAction);
        btnDelete.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        foreach (var button in new[] { btnRefresh, btnInstall, btnUpdate, btnDelete })
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
    }

    private async Task RefreshCatalogAsync()
    {
        SetBusy(true, "Loading releases from windows.php.net…", cancellable: true);
        try
        {
            _releases = await _manager.GetAvailableReleasesAsync(_operationCancellation!.Token);
            _catalogLoaded = true;
            RebuildList();
            lblStatus.Text = $"Loaded {_releases.Count} official Windows x64 NTS releases.";
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Release refresh cancelled.";
        }
        catch (Exception ex)
        {
            _catalogLoaded = false;
            RebuildList();
            lblStatus.Text = $"Could not load the PHP release catalog: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RebuildList(string? selectVersion = null)
    {
        string? selected = selectVersion ?? SelectedRow?.Version;
        var installed = _manager.Scan().ToDictionary(
            item => item.Version, StringComparer.OrdinalIgnoreCase);
        var releases = _releases.ToDictionary(
            item => item.Version, StringComparer.OrdinalIgnoreCase);
        var versions = installed.Keys.Concat(releases.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(ParseVersion)
            .ThenByDescending(value => value, StringComparer.OrdinalIgnoreCase);

        lvVersions.BeginUpdate();
        try
        {
            lvVersions.Items.Clear();
            foreach (string version in versions)
            {
                installed.TryGetValue(version, out var installedVersion);
                releases.TryGetValue(version, out var release);
                var update = installedVersion is null ? null : _manager.GetUpdate(version, _releases);
                bool updateAlreadyInstalled = update is not null && installed.ContainsKey(update.Version);

                string status = GetStatus(version, installedVersion, release, update, updateAlreadyInstalled);
                var row = new VersionRow(version, installedVersion, release,
                    updateAlreadyInstalled ? null : update);
                var item = new ListViewItem(version) { Tag = row };
                item.SubItems.Add(status);
                item.SubItems.Add(release?.PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd") ?? "—");
                item.SubItems.Add(release is null ? "Local / custom" : $"x64 NTS  {release.Size}".TrimEnd());
                lvVersions.Items.Add(item);

                if (version.Equals(selected, StringComparison.OrdinalIgnoreCase))
                    item.Selected = true;
            }
        }
        finally
        {
            lvVersions.EndUpdate();
        }

        if (lvVersions.SelectedItems.Count == 0 && lvVersions.Items.Count > 0)
            lvVersions.Items[0].Selected = true;
        UpdateButtons();
    }

    private string GetStatus(
        string version,
        PhpVersionInfo? installed,
        PhpReleaseInfo? release,
        PhpReleaseInfo? update,
        bool updateAlreadyInstalled)
    {
        if (installed is null) return "Available";

        string current = version.Equals(_currentVersion, StringComparison.OrdinalIgnoreCase)
            ? " · Current" : "";
        if (updateAlreadyInstalled) return $"Installed{current} · Newer patch also installed";
        if (update is not null) return $"Installed{current} · Update to {update.Version}";
        if (_catalogLoaded && release is null) return $"Installed{current} · Local / archived";
        return $"Installed{current}";
    }

    private void UpdateButtons()
    {
        var row = SelectedRow;
        bool idle = _operationCancellation is null;
        int installedCount = _manager.Versions.Count;

        btnRefresh.Enabled = idle;
        btnInstall.Enabled = idle && row?.Release is not null && row.Installed is null;
        btnUpdate.Enabled = idle && row?.Update is not null;
        btnDelete.Enabled = idle && row?.Installed is not null && installedCount > 1 &&
            !row.Version.Equals(_currentVersion, StringComparison.OrdinalIgnoreCase);
        btnClose.Enabled = idle;

        if (!idle) return;
        if (row?.Installed is not null && row.Version.Equals(_currentVersion, StringComparison.OrdinalIgnoreCase))
            lblHint.Text = "The current PHP version cannot be deleted. Switch versions in the main window first.";
        else if (row?.Installed is not null && installedCount == 1)
            lblHint.Text = "The final installed PHP runtime cannot be deleted.";
        else if (row?.Update is not null)
            lblHint.Text = $"Update installs PHP {row.Update.Version} side-by-side so you can switch safely.";
        else
            lblHint.Text = "Official packages are Windows x64 Non Thread Safe builds from php.net.";
    }

    private async void btnRefresh_Click(object sender, EventArgs e) => await RefreshCatalogAsync();

    private async void btnInstall_Click(object sender, EventArgs e)
    {
        if (SelectedRow?.Release is { } release)
            await InstallAsync(release, $"PHP {release.Version}");
    }

    private async void btnUpdate_Click(object sender, EventArgs e)
    {
        if (SelectedRow?.Update is { } update)
            await InstallAsync(update, $"PHP {update.Version} update");
    }

    private async Task InstallAsync(PhpReleaseInfo release, string displayName)
    {
        var progress = new Progress<PhpDownloadProgress>(UpdateProgress);
        SetBusy(true, $"Preparing {displayName}…", cancellable: true);
        try
        {
            await _manager.InstallAsync(release, progress, _operationCancellation!.Token);
            VersionsChanged = true;
            VersionInstalled = true;
            RebuildList(release.Version);
            lblStatus.Text = $"PHP {release.Version} installed. Select it in the main window to activate it.";
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "PHP installation cancelled; temporary files were removed.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PHP Installation Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"PHP {release.Version} was not installed.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UpdateProgress(PhpDownloadProgress progress)
    {
        lblStatus.Text = progress.Stage;
        if (progress.Percentage is { } percentage)
        {
            progressInstall.Style = ProgressBarStyle.Continuous;
            progressInstall.Value = percentage;
            lblStatus.Text = $"{progress.Stage} {FormatBytes(progress.BytesReceived)} / " +
                             $"{FormatBytes(progress.TotalBytes!.Value)} ({percentage}%)";
        }
        else
        {
            progressInstall.Style = ProgressBarStyle.Marquee;
        }
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        var row = SelectedRow;
        if (row?.Installed is null) return;

        if (row.Version.Equals(_currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "Switch to another PHP version in the main window before deleting this one.",
                "PHP Version In Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_manager.Versions.Count <= 1)
        {
            MessageBox.Show(this, "Hamster Server must keep at least one installed PHP runtime.",
                "Cannot Delete PHP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            $"Permanently delete PHP {row.Version} and all files in its runtime folder?\n\nThis cannot be undone.",
            "Delete PHP Version", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        try
        {
            _manager.DeleteVersion(row.Version, _currentVersion);
            VersionsChanged = true;
            RebuildList();
            lblStatus.Text = $"PHP {row.Version} was deleted.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could Not Delete PHP",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetBusy(bool busy, string? status = null, bool cancellable = false)
    {
        if (busy)
        {
            _operationCancellation?.Dispose();
            _operationCancellation = new CancellationTokenSource();
            progressInstall.Visible = true;
            progressInstall.Style = ProgressBarStyle.Marquee;
            progressInstall.Value = 0;
            btnCancel.Visible = cancellable;
            if (status is not null) lblStatus.Text = status;
        }
        else
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            progressInstall.Visible = false;
            btnCancel.Visible = false;
        }
        UpdateButtons();
    }

    private VersionRow? SelectedRow => lvVersions.SelectedItems.Count == 0
        ? null : lvVersions.SelectedItems[0].Tag as VersionRow;

    private static Version ParseVersion(string value) =>
        Version.TryParse(value, out var version) ? version : new Version(0, 0);

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024L * 1024L => $"{bytes / (1024d * 1024d):0.0} MB",
        >= 1024L => $"{bytes / 1024d:0.0} KB",
        _ => $"{bytes} B"
    };

    private void lvVersions_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtons();
    private void btnCancel_Click(object sender, EventArgs e) => _operationCancellation?.Cancel();
    private void btnClose_Click(object sender, EventArgs e) => Close();

    private sealed record VersionRow(
        string Version,
        PhpVersionInfo? Installed,
        PhpReleaseInfo? Release,
        PhpReleaseInfo? Update);
}
