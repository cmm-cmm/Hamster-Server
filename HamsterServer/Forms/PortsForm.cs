using System.Diagnostics;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

/// <summary>
/// Apache's listening ports, and — on the second tab — every port listening on the computer, so a
/// "port already in use" is something the user can see rather than a log line.
/// <para>
/// Both tabs read the same <see cref="SystemPortScanner"/> snapshot: the Apache list says per port
/// whether it is free, Hamster Server's own, or taken by another program; the System list shows all
/// listeners with their program, marking Hamster Server's in green and the ones that block it in red.
/// The scan runs off the UI thread when the window opens and on Refresh / Check availability.
/// </para>
/// </summary>
public partial class PortsForm : Form
{
    private readonly AppConfig _config;
    private readonly string? _rootDir;
    public bool ConfigChanged { get; private set; }

    private const string TagHttpPrimary = "http-primary";
    private const string TagHttpsPrimary = "https-primary";

    // Indices into _portIcons, in the order they are added below.
    private const int IconHttp = 0;
    private const int IconHttps = 1;
    private const int IconHttpOff = 2;
    private const int IconHttpsOff = 3;

    // Indices into the System list's icons.
    private const int UsageHamster = 0;
    private const int UsageConflict = 1;
    private const int UsageOther = 2;

    // Indices into the tab icons.
    private const int TabIconApache = 0;
    private const int TabIconSystem = 1;
    private const int TabIconWarning = 2;

    private ImageList _portIcons = null!;
    private IReadOnlyList<PortUsage> _usage = [];
    private bool _scanned;
    private bool _scanning;
    private string? _scanError;
    private int _sortColumn;
    private bool _sortDescending;

    /// <param name="rootDir">Install root, to recognise this installation's httpd / php-cgi; null treats them as any other program.</param>
    public PortsForm(AppConfig config, string? rootDir = null)
    {
        InitializeComponent();
        _config = config;
        _rootDir = rootDir;
        InitIcons();
        LoadPorts();
        UpdateHint();

        var tt = new ToolTip(components!) { AutoPopDelay = 5000, InitialDelay = 400 };
        tt.SetToolTip(lvPorts, "Tick or untick to enable or disable a port. Double-click to edit.");
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Resize does not fire when the size never changed — size the last columns explicitly.
        lvPorts_Resize(this, EventArgs.Empty);
        lvSystem_Resize(this, EventArgs.Empty);
        await RescanAsync();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        if (keyData == Keys.F5) { _ = RescanAsync(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Icons ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A closed padlock means the port terminates TLS, an open one plain HTTP; grey means switched
    /// off. In the System list: green dot = Hamster Server, red = blocks Hamster Server, grey = any
    /// other program. The System tab's own icon turns into a red warning while something conflicts.
    /// </summary>
    private void InitIcons()
    {
        _portIcons = new ImageList(components) { ImageSize = UiTheme.IconBox, ColorDepth = ColorDepth.Depth32Bit };
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Unlock, UiTheme.AccentHttp));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Lock, UiTheme.AccentHttps));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Unlock, UiTheme.StatusStopped));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Lock, UiTheme.StatusStopped));
        lvPorts.SmallImageList = _portIcons;

        var usageIcons = new ImageList(components) { ImageSize = UiTheme.IconBox, ColorDepth = ColorDepth.Depth32Bit };
        usageIcons.Images.Add(UiTheme.CreateStatusDot(UiTheme.StatusRunning));
        usageIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Warning, UiTheme.StatusError));
        usageIcons.Images.Add(UiTheme.CreateStatusDot(UiTheme.StatusStopped));
        lvSystem.SmallImageList = usageIcons;

        var tabIcons = new ImageList(components) { ImageSize = UiTheme.IconBox, ColorDepth = ColorDepth.Depth32Bit };
        tabIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Ports, UiTheme.LogApache));
        tabIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Server, UiTheme.AccentNeutral));
        tabIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Warning, UiTheme.StatusError));
        tabPorts.ImageList = tabIcons;
        tabApache.ImageIndex = TabIconApache;
        tabSystem.ImageIndex = TabIconSystem;

        btnAdd.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Add, UiTheme.StatusRunning);
        btnEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        btnRemove.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        btnRecheck.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Refresh, UiTheme.AccentAction);
        btnSysRefresh.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Refresh, UiTheme.AccentAction);
        btnSysRelevant.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Warning, UiTheme.StatusTransition);
        foreach (var b in new[] { btnAdd, btnEdit, btnRemove, btnRecheck, btnSysRefresh, btnSysRelevant })
            b.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

        // The right-click menus repeat toolbar actions, so they carry the same glyphs.
        ctxEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        ctxRemove.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        ctxSysBrowse.Image = UiTheme.CreateGlyph(UiTheme.Glyph.World, UiTheme.AccentHttp);
        ctxSysOpenLocation.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Folder, UiTheme.StatusTransition);
        ctxSysCopy.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Page, UiTheme.AccentNeutral);
        UiTheme.ApplyIconScaling(toolStrip, tsSystem, ctxPorts, ctxSystem, statusStrip);
    }

    private static int PortIcon(bool enabled, bool isSsl) =>
        isSsl ? (enabled ? IconHttps : IconHttpsOff)
              : (enabled ? IconHttp : IconHttpOff);

    // ─── Scan ─────────────────────────────────────────────────────────────────

    private async Task RescanAsync()
    {
        if (_scanning || IsDisposed) return;
        _scanning = true;
        btnSysRefresh.Enabled = btnRecheck.Enabled = false;
        lblSysCount.Text = "Scanning…";
        try
        {
            var listeners = await Task.Run(SystemPortScanner.GetTcpListeners);
            _usage = SystemPortScanner.Classify(listeners, _config, _rootDir);
            _scanned = true;
            _scanError = null;
        }
        catch (Exception ex)
        {
            _scanError = ex.Message;
        }
        finally
        {
            _scanning = false;
        }
        if (IsDisposed) return;
        btnSysRefresh.Enabled = btnRecheck.Enabled = true;
        LoadPorts();
        FillSystemList();
        UpdateHint();
    }

    private PortUsage? Holder(int port) =>
        _usage.Where(u => u.Listener.Port == port)
              .OrderBy(u => u.Owner == PortOwnerKind.Conflict ? 0 : 1)   // a conflict wins over our own socket
              .FirstOrDefault();

    /// <summary>The "On this computer" text and colour for one configured port.</summary>
    private (string Text, Color Color) Availability(int port, bool enabled)
    {
        if (!_scanned) return (_scanError is null ? "Checking…" : $"Could not check: {_scanError}", SystemColors.GrayText);
        var holder = Holder(port);
        if (holder is null)
            return enabled ? ("Free", UiTheme.StatusRunning) : ("Free", SystemColors.GrayText);
        var l = holder.Listener;
        return holder.Owner switch
        {
            PortOwnerKind.HamsterApache => ("In use by Hamster Server (Apache running)", UiTheme.StatusRunning),
            PortOwnerKind.HamsterPhp => ("Taken by Hamster Server's own PHP FastCGI", UiTheme.StatusError),
            _ when enabled => ($"Taken by {l.ProcessName} (PID {l.ProcessId}) — Apache cannot start", UiTheme.StatusError),
            _ => ($"Taken by {l.ProcessName} (PID {l.ProcessId})", UiTheme.StatusTransition)
        };
    }

    // ─── Apache ports ─────────────────────────────────────────────────────────

    private void LoadPorts()
    {
        object? selected = lvPorts.SelectedItems.Count > 0 ? lvPorts.SelectedItems[0].Tag : null;
        lvPorts.ItemChecked -= lvPorts_ItemChecked;
        lvPorts.BeginUpdate();
        try
        {
            lvPorts.Items.Clear();
            AddPortRow(_config.Apache.Port, isSsl: false, "Primary HTTP", enabled: true, TagHttpPrimary);
            AddPortRow(_config.Apache.SslPort, isSsl: true, "Primary HTTPS", _config.Apache.EnableSsl, TagHttpsPrimary);
            foreach (var p in _config.Apache.ExtraPorts ?? [])
                AddPortRow(p.Port, p.IsSsl, p.IsSsl ? "Extra HTTPS" : "Extra HTTP", p.Enabled, p);

            foreach (ListViewItem item in lvPorts.Items)
                if (ReferenceEquals(item.Tag, selected) || Equals(item.Tag, selected)) item.Selected = true;
        }
        finally
        {
            lvPorts.EndUpdate();
            lvPorts.ItemChecked += lvPorts_ItemChecked;
            UpdateButtons();
        }
    }

    private void AddPortRow(int port, bool isSsl, string kind, bool enabled, object tag)
    {
        var item = new ListViewItem(port.ToString()) { UseItemStyleForSubItems = false, Tag = tag };
        item.SubItems.Add(isSsl ? "✓" : "—");
        item.SubItems.Add(kind);
        item.SubItems.Add(enabled ? "Enabled" : "Disabled");
        item.SubItems.Add("");
        lvPorts.Items.Add(item);
        item.Checked = enabled;
        StylePortRow(item, port, isSsl, enabled);
    }

    private void StylePortRow(ListViewItem item, int port, bool isSsl, bool enabled)
    {
        var rowColor = enabled ? SystemColors.WindowText : SystemColors.GrayText;
        for (int i = 0; i < 4; i++) item.SubItems[i].ForeColor = rowColor;
        item.SubItems[3].Text = enabled ? "Enabled" : "Disabled";
        item.SubItems[3].ForeColor = enabled ? UiTheme.StatusRunning : SystemColors.GrayText;
        var (text, color) = Availability(port, enabled);
        item.SubItems[4].Text = text;
        item.SubItems[4].ForeColor = color;
        item.ImageIndex = PortIcon(enabled, isSsl);
    }

    private void UpdateHint()
    {
        var parts = new List<string> { $":{_config.Apache.Port}" };
        if (_config.Apache.EnableSsl)
            parts.Add($":{_config.Apache.SslPort} (SSL)");
        foreach (var ep in (_config.Apache.ExtraPorts ?? []).Where(p => p.Enabled))
            parts.Add($":{ep.Port}{(ep.IsSsl ? " (SSL)" : "")}");

        string fcgi = $"PHP FastCGI :{_config.Php.FcgiPort}";
        if (_scanned && Holder(_config.Php.FcgiPort) is { Owner: PortOwnerKind.Conflict } blocker)
            fcgi += $" — taken by {blocker.Listener.ProcessName}!";
        lblHint.Text = "Apache will listen on  " + string.Join("  ·  ", parts) + "     " + fcgi;
    }

    private void UpdateButtons()
    {
        bool hasSelection = lvPorts.SelectedItems.Count > 0;
        btnEdit.Enabled = ctxEdit.Enabled = hasSelection;
        btnRemove.Enabled = ctxRemove.Enabled =
            hasSelection && lvPorts.SelectedItems[0].Tag is PortEntry;
    }

    private void lvPorts_ItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        switch (e.Item.Tag)
        {
            case TagHttpPrimary:
                if (!e.Item.Checked)
                {
                    lvPorts.ItemChecked -= lvPorts_ItemChecked;
                    e.Item.Checked = true;
                    lvPorts.ItemChecked += lvPorts_ItemChecked;
                }
                return;

            case TagHttpsPrimary:
                _config.Apache.EnableSsl = e.Item.Checked;
                StylePortRow(e.Item, _config.Apache.SslPort, isSsl: true, e.Item.Checked);
                break;

            case PortEntry entry:
                entry.Enabled = e.Item.Checked;
                StylePortRow(e.Item, entry.Port, entry.IsSsl, entry.Enabled);
                break;

            default:
                return;
        }
        ConfigChanged = true;
        // The set of ports Hamster Server needs changed, so what counts as a conflict did too.
        if (_scanned) _usage = SystemPortScanner.Classify(_usage.Select(u => u.Listener), _config, _rootDir);
        FillSystemList();
        UpdateHint();
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        using var dlg = PortDialog.ForAdd(_config);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var entry = new PortEntry { Port = dlg.PortValue, IsSsl = dlg.IsSsl, Enabled = true };
        (_config.Apache.ExtraPorts ??= []).Add(entry);
        AfterConfigEdit();
        foreach (ListViewItem item in lvPorts.Items)
            item.Selected = ReferenceEquals(item.Tag, entry);
    }

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (lvPorts.SelectedItems.Count == 0) return;
        var item = lvPorts.SelectedItems[0];

        PortDialog dlg;
        switch (item.Tag)
        {
            case TagHttpPrimary: dlg = PortDialog.ForEditPrimaryHttp(_config); break;
            case TagHttpsPrimary: dlg = PortDialog.ForEditPrimaryHttps(_config); break;
            case PortEntry entry: dlg = PortDialog.ForEditExtra(_config, entry); break;
            default: return;
        }

        using (dlg)
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            switch (item.Tag)
            {
                case TagHttpPrimary:
                    _config.Apache.Port = dlg.PortValue;
                    break;
                case TagHttpsPrimary:
                    _config.Apache.SslPort = dlg.PortValue;
                    _config.Apache.EnableSsl = dlg.IsSsl;
                    break;
                case PortEntry entry:
                    entry.Port = dlg.PortValue;
                    entry.IsSsl = dlg.IsSsl;
                    entry.Enabled = dlg.IsEnabled;
                    break;
            }
        }
        AfterConfigEdit();
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        if (lvPorts.SelectedItems.Count == 0) return;
        if (lvPorts.SelectedItems[0].Tag is not PortEntry entry)
        {
            MessageBox.Show(this, "Primary ports cannot be removed.\nUntick them to disable them instead.",
                "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        (_config.Apache.ExtraPorts ?? []).Remove(entry);
        AfterConfigEdit();
    }

    private void AfterConfigEdit()
    {
        ConfigChanged = true;
        if (_scanned) _usage = SystemPortScanner.Classify(_usage.Select(u => u.Listener), _config, _rootDir);
        LoadPorts();
        FillSystemList();
        UpdateHint();
    }

    private void lvPorts_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtons();
    private void lvPorts_DoubleClick(object sender, EventArgs e) => btnEdit_Click(sender, e);

    private void lvPorts_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete) btnRemove_Click(sender, e);
        if (e.KeyCode == Keys.Return) btnEdit_Click(sender, e);
    }

    // Let the last column absorb the leftover width so the grid has no dead strip on the right.
    private void lvPorts_Resize(object? sender, EventArgs e)
    {
        int used = colPort.Width + colSsl.Width + colKind.Width + colStatus.Width;
        colMachine.Width = Math.Max(160, lvPorts.ClientSize.Width - used);
    }

    // ─── System ports ─────────────────────────────────────────────────────────

    private void FillSystemList()
    {
        string filter = txtSysFilter.Text.Trim();
        bool relevantOnly = btnSysRelevant.Checked;
        var rows = _usage
            .Where(u => !relevantOnly || u.Owner != PortOwnerKind.Other)
            .Where(u => filter.Length == 0 || Matches(u, filter));
        rows = (_sortColumn, _sortDescending) switch
        {
            (1, false) => rows.OrderBy(u => AddressText(u.Listener)),
            (1, true) => rows.OrderByDescending(u => AddressText(u.Listener)),
            (2, false) => rows.OrderBy(u => u.Listener.ProcessName, StringComparer.OrdinalIgnoreCase),
            (2, true) => rows.OrderByDescending(u => u.Listener.ProcessName, StringComparer.OrdinalIgnoreCase),
            (3, false) => rows.OrderBy(u => u.Listener.ProcessId),
            (3, true) => rows.OrderByDescending(u => u.Listener.ProcessId),
            (4, false) => rows.OrderBy(u => (int)u.Owner == 0 ? 3 : (int)u.Owner).ThenBy(u => UseText(u)),
            (4, true) => rows.OrderByDescending(u => (int)u.Owner == 0 ? 3 : (int)u.Owner).ThenBy(u => UseText(u)),
            (_, true) => rows.OrderByDescending(u => u.Listener.Port),
            _ => rows.OrderBy(u => u.Listener.Port)
        };
        var shown = rows.ToList();

        lvSystem.BeginUpdate();
        try
        {
            lvSystem.Items.Clear();
            foreach (var u in shown)
            {
                var l = u.Listener;
                var item = new ListViewItem(l.Port.ToString(), u.Owner switch
                {
                    PortOwnerKind.HamsterApache or PortOwnerKind.HamsterPhp => UsageHamster,
                    PortOwnerKind.Conflict => UsageConflict,
                    _ => UsageOther
                })
                {
                    Tag = u,
                    ToolTipText = l.ProcessPath ?? $"{l.ProcessName} — path not readable without administrator rights"
                };
                item.SubItems.Add(AddressText(l));
                item.SubItems.Add(l.ProcessName);
                item.SubItems.Add(l.ProcessId.ToString());
                item.SubItems.Add(UseText(u));
                item.ForeColor = u.Owner switch
                {
                    PortOwnerKind.Conflict => UiTheme.StatusError,
                    PortOwnerKind.HamsterApache or PortOwnerKind.HamsterPhp => UiTheme.StatusRunning,
                    _ => SystemColors.WindowText
                };
                lvSystem.Items.Add(item);
            }
        }
        finally
        {
            lvSystem.EndUpdate();
        }

        int conflicts = _usage.Count(u => u.Owner == PortOwnerKind.Conflict);
        tabSystem.ImageIndex = conflicts > 0 ? TabIconWarning : TabIconSystem;
        tabSystem.Text = conflicts > 0 ? $"System ports ({conflicts} conflict{(conflicts == 1 ? "" : "s")})" : "System ports";
        lblSysCount.Text = !_scanned
            ? (_scanError is null ? "" : $"Could not read ports: {_scanError}")
            : $"{shown.Count} of {_usage.Count} listening ports";
    }

    private static bool Matches(PortUsage u, string filter) =>
        u.Listener.Port.ToString().Contains(filter, StringComparison.Ordinal) ||
        u.Listener.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        UseText(u).Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static string AddressText(ListeningPort l)
    {
        if (l.Addresses.All(a => a is "0.0.0.0" or "::")) return "All networks";
        if (l.LocalOnly) return "This computer only";
        return string.Join(", ", l.Addresses);
    }

    private static string UseText(PortUsage u) => u.Owner switch
    {
        PortOwnerKind.HamsterApache => $"Hamster Server · {u.Role ?? "Apache"}",
        PortOwnerKind.HamsterPhp => "Hamster Server · PHP FastCGI",
        PortOwnerKind.Conflict => $"Blocks Hamster Server: {u.Role}",
        _ => u.WellKnownService ?? ""
    };

    private void txtSysFilter_TextChanged(object? sender, EventArgs e) => FillSystemList();

    private async void btnSysRefresh_Click(object? sender, EventArgs e) => await RescanAsync();

    private void tabPorts_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (tabPorts.SelectedTab == tabSystem) lvSystem_Resize(this, EventArgs.Empty);
    }

    private void lvSystem_ColumnClick(object? sender, ColumnClickEventArgs e)
    {
        _sortDescending = _sortColumn == e.Column && !_sortDescending;
        _sortColumn = e.Column;
        FillSystemList();
    }

    private void lvSystem_Resize(object? sender, EventArgs e)
    {
        int used = colSysPort.Width + colSysAddress.Width + colSysProcess.Width + colSysPid.Width;
        colSysUse.Width = Math.Max(160, lvSystem.ClientSize.Width - used);
    }

    private PortUsage? SelectedSystemRow =>
        lvSystem.SelectedItems.Count > 0 ? lvSystem.SelectedItems[0].Tag as PortUsage : null;

    private void ctxSystem_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var row = SelectedSystemRow;
        if (row is null) { e.Cancel = true; return; }
        ctxSysOpenLocation.Enabled = row.Listener.ProcessPath is not null;
    }

    private void ctxSysBrowse_Click(object? sender, EventArgs e)
    {
        if (SelectedSystemRow is not { } row) return;
        string scheme = row.Listener.Port is 443 or 8443 ? "https" : "http";
        Process.Start(new ProcessStartInfo($"{scheme}://localhost:{row.Listener.Port}/") { UseShellExecute = true });
    }

    private void ctxSysOpenLocation_Click(object? sender, EventArgs e)
    {
        if (SelectedSystemRow?.Listener.ProcessPath is not { } path || !File.Exists(path)) return;
        Process.Start("explorer.exe", $"/select,\"{path}\"");
    }

    private void ctxSysCopy_Click(object? sender, EventArgs e)
    {
        if (SelectedSystemRow is not { } row) return;
        var l = row.Listener;
        Clipboard.SetText($"Port {l.Port}\t{string.Join(", ", l.Addresses)}\t{l.ProcessName}\tPID {l.ProcessId}\t{l.ProcessPath}\t{UseText(row)}");
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();
}
