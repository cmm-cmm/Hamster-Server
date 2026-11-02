using System.Text;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

/// <summary>
/// Installs, updates, activates and removes PHP runtimes.
/// <para>
/// The summary strip answers "which PHP am I on and is it current?" first; the list below is grouped
/// into installed and downloadable runtimes with an icon per state; the details box spells out what
/// the selected row means — including which extensions a runtime is missing and where they can be
/// copied from. Patch updates go through <see cref="PhpUpdateService"/>, the same path the background
/// updater takes, so a manual update also carries user-added extensions (sqlsrv, xdebug…) over.
/// </para>
/// </summary>
public partial class PhpVersionManagerForm : Form
{
    private const int IconActive = 0;
    private const int IconActiveOutdated = 1;
    private const int IconInstalled = 2;
    private const int IconAvailable = 3;

    private readonly PhpVersionManager _manager;
    private readonly AppConfig _config;
    private readonly PhpUpdateService? _updates;
    private readonly Func<string, Task>? _switchAsync;
    private readonly ListViewGroup _groupInstalled = new("installed", "Installed");
    private readonly ListViewGroup _groupAvailable = new("available", "Available from windows.php.net");
    private IReadOnlyList<PhpReleaseInfo> _releases = [];
    private CancellationTokenSource? _operationCancellation;
    private bool _catalogLoaded;
    private string? _catalogError;
    private DateTimeOffset? _catalogTime;
    private bool _loadingMode;

    public bool VersionsChanged { get; private set; }
    /// <summary>A runtime was installed or received extension files: php.ini must be regenerated.</summary>
    public bool RuntimeFilesChanged { get; private set; }
    /// <summary>The update mode changed; the caller saves the configuration.</summary>
    public bool ConfigChanged { get; private set; }

    /// <param name="updates">The shared updater; null (tests) falls back to plain install + copy.</param>
    /// <param name="switchAsync">Activates a runtime (stop, switch, start, save); null hides "Use this version".</param>
    public PhpVersionManagerForm(
        PhpVersionManager manager,
        AppConfig config,
        PhpUpdateService? updates = null,
        Func<string, Task>? switchAsync = null)
    {
        InitializeComponent();
        _manager = manager;
        _config = config;
        _updates = updates;
        _switchAsync = switchAsync;
        btnUse.Visible = switchAsync is not null;

        lvVersions.Groups.Add(_groupInstalled);
        lvVersions.Groups.Add(_groupAvailable);
        InitIcons();

        _loadingMode = true;
        cmbUpdateMode.SelectedIndex = Math.Clamp((int)config.Php.UpdateMode, 0, cmbUpdateMode.Items.Count - 1);
        _loadingMode = false;

        if (_updates is not null) _updates.StateChanged += OnUpdaterStateChanged;
        FormClosed += (_, _) =>
        {
            if (_updates is not null) _updates.StateChanged -= OnUpdaterStateChanged;
        };

        RebuildList();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        lvVersions_Resize(this, EventArgs.Empty);
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

    private string ActiveVersion => _config.Php.CurrentVersion;

    // ─── Icons ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Green dot = the active runtime, amber download = active but a newer patch exists, grey dot =
    /// installed, blue download = can be installed. The Status column says the same in words.
    /// </summary>
    private void InitIcons()
    {
        var icons = new ImageList(components) { ImageSize = UiTheme.IconBox, ColorDepth = ColorDepth.Depth32Bit };
        icons.Images.Add(UiTheme.CreateStatusDot(UiTheme.StatusRunning));
        icons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Download, UiTheme.StatusTransition));
        icons.Images.Add(UiTheme.CreateStatusDot(UiTheme.StatusStopped));
        icons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Download, UiTheme.AccentAction));
        lvVersions.SmallImageList = icons;

        btnRefresh.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Refresh, UiTheme.AccentAction);
        btnInstall.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Add, UiTheme.StatusRunning);
        btnUpdate.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Download, UiTheme.StatusTransition);
        btnUse.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Accept, UiTheme.StatusRunning);
        btnCopyExtensions.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Code, UiTheme.LogPhp);
        btnDelete.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        btnCleanup.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Clear, UiTheme.StatusError);
        foreach (var button in new[] { btnRefresh, btnInstall, btnUpdate, btnUse, btnCopyExtensions, btnDelete, btnCleanup })
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        UiTheme.ApplyIconScaling(toolStrip);

        UiTheme.ApplyGlyph(btnUpdateActive, UiTheme.Glyph.Download, UiTheme.StatusTransition);
    }

    // ─── Catalog ──────────────────────────────────────────────────────────────

    private async Task RefreshCatalogAsync()
    {
        SetBusy(true, "Checking windows.php.net for releases…", cancellable: true);
        try
        {
            _releases = await _manager.GetAvailableReleasesAsync(_operationCancellation!.Token);
            _catalogLoaded = true;
            _catalogError = null;
            _catalogTime = DateTimeOffset.Now;
            lblStatus.Text = $"{_releases.Count} official Windows x64 NTS releases found.";
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Check cancelled.";
        }
        catch (Exception ex)
        {
            _catalogLoaded = false;
            _catalogError = ex.Message;
            lblStatus.Text = $"Could not reach windows.php.net: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
            RebuildList();
        }
    }

    // ─── List ─────────────────────────────────────────────────────────────────

    private void RebuildList(string? selectVersion = null)
    {
        string? selected = selectVersion ?? SelectedRow?.Version;
        var installed = _manager.Scan().ToDictionary(item => item.Version, StringComparer.OrdinalIgnoreCase);
        var releases = _releases
            .GroupBy(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var superseded = _manager.GetSupersededVersions(ActiveVersion)
            .Select(v => v.Version).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var enabled = (_config.Php.EnabledExtensions ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = new List<VersionRow>();
        foreach (var (version, runtime) in installed)
        {
            releases.TryGetValue(version, out var release);
            var update = _manager.GetUpdate(version, _releases);
            bool updateInstalled = update is not null && installed.ContainsKey(update.Version);
            var available = PhpExtensionManager.GetAvailableExtensions(runtime.DirectoryPath);
            var missingEnabled = enabled.Except(available, StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            rows.Add(new VersionRow(version, runtime, release, update, updateInstalled,
                superseded.Contains(version), available.Count, missingEnabled, _manager.FindMissingFiles(version)));
        }
        foreach (var (version, release) in releases.Where(r => !installed.ContainsKey(r.Key)))
            rows.Add(new VersionRow(version, null, release, null, false, false, 0, [], []));

        lvVersions.BeginUpdate();
        try
        {
            lvVersions.Items.Clear();
            foreach (var row in rows.OrderByDescending(r => PhpVersionKey.ParseOrZero(r.Version))
                                    .ThenByDescending(r => r.Version, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(row.Version, IconFor(row))
                {
                    Tag = row,
                    Group = row.Installed is null ? _groupAvailable : _groupInstalled
                };
                item.SubItems.Add(StatusText(row));
                item.SubItems.Add(ExtensionsText(row));
                item.SubItems.Add(row.Release?.PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd") ?? "—");
                item.SubItems.Add(string.IsNullOrWhiteSpace(row.Release?.Size) ? "—" : row.Release!.Size);
                lvVersions.Items.Add(item);
                if (row.Version.Equals(selected, StringComparison.OrdinalIgnoreCase)) item.Selected = true;
            }
        }
        finally
        {
            lvVersions.EndUpdate();
        }

        if (SelectedRow is null && lvVersions.Items.Count > 0)
        {
            var active = lvVersions.Items.Cast<ListViewItem>()
                .FirstOrDefault(i => ((VersionRow)i.Tag!).Version.Equals(ActiveVersion, StringComparison.OrdinalIgnoreCase));
            (active ?? lvVersions.Items[0]).Selected = true;
        }
        UpdateSummary();
        UpdateButtons();
    }

    private int IconFor(VersionRow row)
    {
        if (row.Installed is null) return IconAvailable;
        if (!IsActive(row)) return IconInstalled;
        return row.Update is not null && !row.UpdateInstalled ? IconActiveOutdated : IconActive;
    }

    private string StatusText(VersionRow row)
    {
        if (row.Installed is null)
        {
            var sibling = _manager.Versions.FirstOrDefault(v => PhpVersionManager.IsSameBranch(v.Version, row.Version));
            return sibling is not null
                ? $"Newer patch for your PHP {Branch(row.Version)}"
                : $"PHP {Branch(row.Version)} branch, not installed";
        }

        var text = new StringBuilder(IsActive(row) ? "Active" : row.Superseded ? "Older patch, kept for rollback" : "Installed");
        if (row.Update is not null)
            text.Append(row.UpdateInstalled ? $" · {row.Update.Version} also installed" : $" · update {row.Update.Version} available");
        else if (_catalogLoaded && row.Release is null && !row.Superseded && PhpVersionKey.IsValid(row.Version))
            text.Append(" · not on php.net any more");
        return text.ToString();
    }

    private static string ExtensionsText(VersionRow row)
    {
        if (row.Installed is null) return "—";
        var text = new StringBuilder($"{row.ExtensionCount} available");
        if (row.MissingEnabled.Count > 0) text.Append($" · {row.MissingEnabled.Count} missing");
        if (row.Transferable.Count > 0) text.Append($" · {row.Transferable.Count} can be copied");
        return text.ToString();
    }

    // ─── Summary and details ──────────────────────────────────────────────────

    private void OnUpdaterStateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (_updates?.State is PhpUpdateState.Updated) RebuildList();
        else UpdateSummary();
        UpdateButtons();
    }

    private PhpReleaseInfo? ActiveUpdate => _manager.GetUpdate(ActiveVersion, _releases);

    private void UpdateSummary()
    {
        lblActiveTitle.Text = string.IsNullOrWhiteSpace(ActiveVersion)
            ? "No PHP runtime is active"
            : $"PHP {ActiveVersion} is active";

        var update = ActiveUpdate;
        bool updateInstalled = update is not null && _manager.GetVersion(update.Version) is not null;
        btnUpdateActive.Visible = update is not null;
        if (update is not null)
            btnUpdateActive.Text = updateInstalled ? $"Switch to {update.Version}" : $"Update to {update.Version}";

        string detail;
        Color color;
        if (_updates?.State is PhpUpdateState.Installing or PhpUpdateState.WaitingToSwitch)
        {
            detail = _updates.State == PhpUpdateState.Installing
                ? $"Installing PHP {_updates.AvailableUpdate?.Version} in the background…"
                : $"PHP {_updates.AvailableUpdate?.Version} is installed; switching as soon as no request is in flight.";
            color = UiTheme.StatusTransition;
        }
        else if (_operationCancellation is not null && !_catalogLoaded)
        {
            detail = "Checking windows.php.net…";
            color = SystemColors.GrayText;
        }
        else if (!_catalogLoaded)
        {
            detail = _catalogError is null
                ? "Release list not loaded yet."
                : "windows.php.net could not be reached — showing installed runtimes only.";
            color = SystemColors.GrayText;
        }
        else if (update is not null)
        {
            string date = update.PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd") ?? "";
            detail = updateInstalled
                ? $"PHP {update.Version} is already installed next to it but not active yet."
                : $"PHP {update.Version} is available{(date.Length > 0 ? $" ({date})" : "")}. Extensions you added are carried over.";
            color = UiTheme.StatusTransition;
        }
        else if (_releases.Any(r => PhpVersionManager.IsSameBranch(r.Version, ActiveVersion)))
        {
            detail = $"Latest PHP {Branch(ActiveVersion)} patch · checked {_catalogTime?.ToLocalTime():HH:mm}";
            color = UiTheme.StatusRunning;
        }
        else
        {
            detail = $"PHP {Branch(ActiveVersion)} is no longer published on windows.php.net; consider a newer branch.";
            color = UiTheme.StatusTransition;
        }
        lblActiveDetail.Text = detail;
        lblActiveDetail.ForeColor = color;
    }

    private void UpdateDetails()
    {
        var row = SelectedRow;
        if (row is null)
        {
            lblDetails.Text = "Select a version.";
            return;
        }

        var text = new StringBuilder();
        if (row.Installed is null)
        {
            var release = row.Release!;
            text.AppendLine($"Official PHP {release.Version} for Windows (x64, Non Thread Safe), {release.Size}, " +
                            $"released {release.PublishedAt?.ToLocalTime():yyyy-MM-dd}.");
            if (PhpVersionKey.TryParse(release.Version, out var key) && key.Build > 0)
                text.AppendLine($"“-{key.Build}” marks Windows rebuild {key.Build} of PHP {key.Version}: the same PHP " +
                                "version with rebuilt binaries (bundled libraries such as OpenSSL updated).");
            text.AppendLine("Downloaded from php.net and verified with SHA-256 before it is installed side-by-side.");
            var sibling = _manager.Versions.FirstOrDefault(v => PhpVersionManager.IsSameBranch(v.Version, release.Version));
            text.AppendLine(sibling is not null
                ? $"Extension DLLs you added to PHP {sibling.Version} are copied into it."
                : $"Extensions you added to other branches are not copied: extension DLLs are built per PHP branch (major.minor).");
        }
        else
        {
            text.AppendLine($"Folder: {row.Installed.DirectoryPath}");
            text.Append(row.ExtensionCount == 1 ? "1 extension in ext\\. " : $"{row.ExtensionCount} extensions in ext\\. ");
            text.AppendLine(row.MissingEnabled.Count > 0
                ? $"Enabled in PHP Configuration but not in this runtime: {string.Join(", ", row.MissingEnabled)}."
                : "Every extension enabled in PHP Configuration is present.");
            if (row.Transferable.Count > 0)
            {
                var sources = string.Join(", ", row.Transferable.Select(f => f.SourceVersion).Distinct());
                text.AppendLine($"Can be copied from PHP {sources} (same branch): " +
                                string.Join(", ", row.Transferable.Select(f => Path.GetFileName(f.RelativePath))) +
                                " — use Copy extensions.");
            }
            if (row.MissingEnabled.Count > 0 && row.Transferable.Count == 0)
                text.AppendLine("Missing extensions must be built for this branch: download the matching DLL " +
                                "(e.g. Microsoft's sqlsrv release for this PHP version, NTS x64) into ext\\.");
        }
        lblDetails.Text = text.ToString().TrimEnd();
    }

    private void UpdateButtons()
    {
        var row = SelectedRow;
        bool idle = _operationCancellation is null && _updates?.IsBusy != true;
        bool installed = row?.Installed is not null;
        bool active = row is not null && IsActive(row);

        btnRefresh.Enabled = _operationCancellation is null;
        btnInstall.Enabled = idle && row?.Release is not null && !installed;
        btnUpdate.Enabled = idle && installed && row!.Update is not null && !row.UpdateInstalled;
        btnUse.Enabled = idle && installed && !active;
        btnCopyExtensions.Enabled = idle && installed && row!.Transferable.Count > 0;
        btnDelete.Enabled = idle && installed && !active && _manager.Versions.Count > 1;
        btnCleanup.Enabled = idle && _manager.GetSupersededVersions(ActiveVersion).Count > 0;
        btnUpdateActive.Enabled = idle;
        cmbUpdateMode.Enabled = _operationCancellation is null;
        btnClose.Enabled = _operationCancellation is null;

        UpdateDetails();
        if (_updates?.IsBusy == true)
            lblHint.Text = "A background update is running; actions are available again when it finishes.";
        else if (row is null)
            lblHint.Text = "Official Windows x64 Non Thread Safe builds from php.net, verified with SHA-256.";
        else if (active)
            lblHint.Text = "The active runtime cannot be removed. Updates install side-by-side, so you can switch back at any time.";
        else if (installed)
            lblHint.Text = "Double-click or use “Use this version” to make it active.";
        else
            lblHint.Text = "Installing does not change the active runtime unless it is a newer patch of it.";
    }

    // ─── Actions ──────────────────────────────────────────────────────────────

    private async void btnRefresh_Click(object sender, EventArgs e) => await RefreshCatalogAsync();

    private async void btnInstall_Click(object sender, EventArgs e)
    {
        if (SelectedRow?.Release is { } release) await InstallAsync(release, fromVersion: null);
    }

    private async void btnUpdate_Click(object sender, EventArgs e)
    {
        if (SelectedRow is { Installed: not null, Update: { } update } row)
            await InstallAsync(update, row.Version);
    }

    private async void btnUpdateActive_Click(object sender, EventArgs e)
    {
        if (ActiveUpdate is { } update) await InstallAsync(update, ActiveVersion);
    }

    /// <summary>
    /// Installs <paramref name="release"/>. A newer patch of an installed branch goes through the
    /// updater: the extension DLLs of the runtime it replaces are copied in, and when that runtime is
    /// the active one the new patch becomes active. Another branch installs alone.
    /// </summary>
    /// <param name="fromVersion">The runtime this updates; null picks the active one when it is the same branch.</param>
    private async Task InstallAsync(PhpReleaseInfo release, string? fromVersion)
    {
        fromVersion ??= PhpVersionManager.IsSameBranch(ActiveVersion, release.Version) &&
                        PhpVersionKey.ParseOrZero(release.Version) > PhpVersionKey.ParseOrZero(ActiveVersion)
            ? ActiveVersion
            : _manager.Versions.FirstOrDefault(v => PhpVersionManager.IsSameBranch(v.Version, release.Version))?.Version;

        var progress = new Progress<PhpDownloadProgress>(UpdateProgress);
        SetBusy(true, $"Preparing PHP {release.Version}…", cancellable: true);
        try
        {
            if (fromVersion is not null && _updates is not null)
            {
                bool ok = await _updates.ApplyUpdateAsync(fromVersion, release, progress,
                    switchImmediately: true, _operationCancellation!.Token);
                if (!ok) throw new InvalidOperationException(_updates.LastError ?? "The update failed.");
            }
            else
            {
                if (_manager.GetVersion(release.Version) is null)
                    await _manager.InstallAsync(release, progress, _operationCancellation!.Token);
                if (fromVersion is not null) _manager.CopyMissingFiles(release.Version, fromVersion);
            }

            VersionsChanged = true;
            RuntimeFilesChanged = true;
            lblStatus.Text = ActiveVersion.Equals(release.Version, StringComparison.OrdinalIgnoreCase)
                ? $"PHP {release.Version} installed and active. {fromVersion} is kept for rollback."
                : $"PHP {release.Version} installed. Use “Use this version” to activate it.";
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Installation cancelled; temporary files were removed.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PHP Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"PHP {release.Version} was not installed.";
        }
        finally
        {
            SetBusy(false);
            RebuildList(release.Version);
        }
    }

    private async void btnUse_Click(object sender, EventArgs e) => await UseSelectedAsync();

    private async void lvVersions_DoubleClick(object sender, EventArgs e)
    {
        if (btnUse.Enabled) await UseSelectedAsync();
    }

    private async Task UseSelectedAsync()
    {
        if (_switchAsync is null || SelectedRow is not { Installed: not null } row || IsActive(row)) return;

        // A runtime of another branch loads other extension DLLs; say what it lacks before switching.
        if (row.MissingEnabled.Count > 0)
        {
            var answer = MessageBox.Show(this,
                $"PHP {row.Version} does not have these enabled extensions: {string.Join(", ", row.MissingEnabled)}.\n\n" +
                "PHP will start without them. Switch anyway?",
                "Missing Extensions", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
        }

        SetBusy(true, $"Switching to PHP {row.Version}…", cancellable: false);
        try
        {
            await _switchAsync(row.Version);
            VersionsChanged = true;
            lblStatus.Text = $"PHP {row.Version} is now active.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could Not Switch PHP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RebuildList(row.Version);
        }
    }

    private void btnCopyExtensions_Click(object sender, EventArgs e)
    {
        if (SelectedRow is not { Installed: not null } row || row.Transferable.Count == 0) return;
        try
        {
            var copied = _manager.CopyMissingFiles(row.Version);
            RuntimeFilesChanged = true;
            lblStatus.Text = copied.Count == 0
                ? "Nothing to copy."
                : $"Copied into PHP {row.Version}: {string.Join(", ", copied.Select(Path.GetFileName))}." +
                  (IsActive(row) ? " Restart PHP to load them." : "");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could Not Copy Extensions", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RebuildList(row.Version);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        var row = SelectedRow;
        if (row?.Installed is null || IsActive(row)) return;

        var answer = MessageBox.Show(this,
            $"Permanently delete PHP {row.Version} and everything in its folder, including extension DLLs you added?\n\nThis cannot be undone.",
            "Remove PHP Version", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        DeleteVersions([row.Version]);
    }

    private void btnCleanup_Click(object sender, EventArgs e)
    {
        var old = _manager.GetSupersededVersions(ActiveVersion).Select(v => v.Version).ToList();
        if (old.Count == 0) return;

        var answer = MessageBox.Show(this,
            $"Delete these older patches? A newer patch of the same branch is installed for each:\n\n  {string.Join("\n  ", old)}\n\n" +
            "Check the newer patch has the extensions you need first (the Extensions column shows “can be copied” when it does not). " +
            "This cannot be undone.",
            "Remove Old Patches", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        DeleteVersions(old);
    }

    private void DeleteVersions(IReadOnlyList<string> toDelete)
    {
        var removed = new List<string>();
        foreach (var version in toDelete)
        {
            try
            {
                _manager.DeleteVersion(version, ActiveVersion);
                removed.Add(version);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"PHP {version}: {ex.Message}", "Could Not Remove PHP",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        if (removed.Count > 0)
        {
            VersionsChanged = true;
            lblStatus.Text = $"Removed PHP {string.Join(", ", removed)}.";
        }
        RebuildList();
    }

    private void cmbUpdateMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_loadingMode || cmbUpdateMode.SelectedIndex < 0) return;
        var mode = (PhpUpdateMode)cmbUpdateMode.SelectedIndex;
        if (mode == _config.Php.UpdateMode) return;
        _config.Php.UpdateMode = mode;
        ConfigChanged = true;
        lblStatus.Text = mode switch
        {
            PhpUpdateMode.Auto => "New patches of the active branch will be installed and activated automatically.",
            PhpUpdateMode.Notify => "You will be told about new patches; nothing is installed on its own.",
            _ => "Hamster Server will not check php.net on its own."
        };
    }

    // ─── Plumbing ─────────────────────────────────────────────────────────────

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
        UpdateSummary();
        UpdateButtons();
    }

    // Let the last column absorb the leftover width so the grid has no dead strip on the right.
    private void lvVersions_Resize(object? sender, EventArgs e)
    {
        int used = 0;
        for (int i = 0; i < lvVersions.Columns.Count - 1; i++) used += lvVersions.Columns[i].Width;
        colSize.Width = Math.Max(80, lvVersions.ClientSize.Width - used);
    }

    private bool IsActive(VersionRow row) => row.Version.Equals(ActiveVersion, StringComparison.OrdinalIgnoreCase);

    // ListView.SelectedItems stays empty until the native control exists, so a row selected while
    // the form is being built read as "nothing selected" — the details box said "Select a version"
    // and RebuildList re-selected the first row. ListViewItem.Selected is kept either way.
    private VersionRow? SelectedRow =>
        (lvVersions.SelectedItems.Count > 0
            ? lvVersions.SelectedItems[0]
            : lvVersions.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Selected))?.Tag as VersionRow;

    private static string Branch(string version) =>
        PhpVersionKey.TryParse(version, out var key) ? key.Branch : version;

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
        PhpReleaseInfo? Update,
        bool UpdateInstalled,
        bool Superseded,
        int ExtensionCount,
        IReadOnlyList<string> MissingEnabled,
        IReadOnlyList<TransferableFile> Transferable);
}
