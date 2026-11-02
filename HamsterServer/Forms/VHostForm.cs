using System.Diagnostics;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class VHostForm : Form
{
    private readonly AppConfig _config;
    private readonly string _rootDir;
    public bool ConfigChanged { get; private set; }
    public bool NeedsPhpRestart { get; private set; }

    private const int IconEnabled = 0;
    private const int IconDisabled = 1;
    private const int IconProxy = 2;

    private ImageList _vhostIcons = null!;
    // Read once per window: names the hosts file points at this computer.
    private readonly IReadOnlySet<string> _loopbackNames = HostsFile.ReadLoopbackNames();

    public VHostForm(AppConfig config, string rootDir)
    {
        InitializeComponent();
        _config = config;
        _rootDir = rootDir;

        InitIcons();
        LoadVHosts();
        lvVHosts_Resize(null!, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Icons ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Green globe = serving files, grey = disabled, blue link = proxied to another server
    /// (Hyperf/Swoole), which behaves differently enough to be worth distinguishing at a glance.
    /// </summary>
    private void InitIcons()
    {
        _vhostIcons = new ImageList(components) { ImageSize = UiTheme.IconBox, ColorDepth = ColorDepth.Depth32Bit };
        _vhostIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.World, UiTheme.StatusRunning));
        _vhostIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.World, UiTheme.StatusStopped));
        _vhostIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Link, UiTheme.AccentHttp));
        lvVHosts.SmallImageList = _vhostIcons;

        btnAdd.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Add, UiTheme.StatusRunning);
        btnEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        btnDelete.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        btnBrowse.Image = UiTheme.CreateGlyph(UiTheme.Glyph.World, UiTheme.AccentHttp);
        btnOpenFolder.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Folder, UiTheme.StatusTransition);
        foreach (var b in new[] { btnAdd, btnEdit, btnDelete, btnToggle, btnBrowse, btnOpenFolder })
            b.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

        // The right-click menu repeats toolbar actions, so it carries the same glyphs. The Enable /
        // Disable pair is set per selection in UpdateButtons.
        ctxEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        ctxDelete.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        ctxBrowse.Image = btnBrowse.Image;
        ctxOpenFolder.Image = btnOpenFolder.Image;
        _enableGlyph = UiTheme.CreateGlyph(UiTheme.Glyph.Play, UiTheme.StatusRunning);
        _disableGlyph = UiTheme.CreateGlyph(UiTheme.Glyph.Stop, UiTheme.StatusError);
        UiTheme.ApplyIconScaling(toolStrip, ctxVHosts);
    }

    private Image _enableGlyph = null!;
    private Image _disableGlyph = null!;

    // ─── ListView painting ────────────────────────────────────────────────────
    // Owner-drawn only so the Status colour survives selection — see UiTheme.DrawStatusListSubItem.

    private void lvVHosts_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) =>
        e.DrawDefault = true;

    private void lvVHosts_DrawItem(object sender, DrawListViewItemEventArgs e) { }

    private void lvVHosts_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) =>
        UiTheme.DrawStatusListSubItem(lvVHosts, e);

    // ─── ListView resize ──────────────────────────────────────────────────────

    private void lvVHosts_Resize(object sender, EventArgs e)
    {
        // The server name is what the user scans for, so it gets the larger share of the flexible
        // width; at the default window size 38 % clipped ordinary names like "blog.localhost".
        int total = lvVHosts.ClientSize.Width - 4;
        int flex = total - 110 - 55 - 130 - 160;  // Alias + Port + Framework + Status
        if (flex < 80) return;
        colServerName.Width = (int)(flex * 0.45);
        colAlias.Width = 110;
        colPort.Width = 55;
        colFramework.Width = 130;
        colDocRoot.Width = (int)(flex * 0.55);
        colEnabled.Width = 160;
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    private void LoadVHosts()
    {
        lvVHosts.Items.Clear();
        foreach (var v in _config.VHosts)
            lvVHosts.Items.Add(MakeListItem(v));
        UpdateButtons();
    }

    private ListViewItem MakeListItem(VHostEntry v)
    {
        var portDisplay = (v.Port > 0 ? v.Port : _config.Apache.Port).ToString();
        var profile = FrameworkProfiles.Get(v.Framework);
        var fwkDisplay = v.Framework == FrameworkPreset.None ? "" : profile.DisplayName;
        var rootDisplay = profile.IsReverseProxy
            ? (string.IsNullOrWhiteSpace(v.DocumentRoot) ? "→ :9501" : $"→ {v.DocumentRoot}")
            : (string.IsNullOrWhiteSpace(v.DocumentRoot) ? "(auto)" : v.DocumentRoot);

        var item = new ListViewItem(v.ServerName) { UseItemStyleForSubItems = false };
        item.SubItems.Add(string.IsNullOrWhiteSpace(v.ServerAlias) ? "" : v.ServerAlias);
        item.SubItems.Add(portDisplay);
        item.SubItems.Add(fwkDisplay);
        item.SubItems.Add(rootDisplay);
        bool reachable = HostsFile.PointsHere(v.ServerName, _loopbackNames);
        item.SubItems.Add(!v.Enabled ? "Disabled" : reachable ? "Active" : "Active · not in hosts file");
        item.Tag = v;
        item.ForeColor = v.Enabled ? SystemColors.WindowText : SystemColors.GrayText;
        // Same vocabulary as the Services list: green is serving, grey is switched off, amber means
        // Apache serves it but the browser will not find it until the name points at 127.0.0.1.
        item.SubItems[5].ForeColor = !v.Enabled ? UiTheme.StatusStopped
                                   : reachable ? UiTheme.StatusRunning
                                   : UiTheme.StatusTransition;
        item.ImageIndex = !v.Enabled ? IconDisabled
                        : profile.IsReverseProxy ? IconProxy
                        : IconEnabled;
        return item;
    }

    private VHostEntry? Selected => lvVHosts.SelectedItems.Count > 0 ? lvVHosts.SelectedItems[0].Tag as VHostEntry : null;

    private void UpdateButtons()
    {
        var v = Selected;
        bool has = v is not null;
        btnEdit.Enabled = ctxEdit.Enabled = has;
        btnDelete.Enabled = ctxDelete.Enabled = has;
        btnToggle.Enabled = ctxToggle.Enabled = has;
        btnBrowse.Enabled = ctxBrowse.Enabled = has && v!.Enabled;
        btnOpenFolder.Enabled = ctxOpenFolder.Enabled = has && Directory.Exists(v!.DocumentRoot);

        bool enabling = v is { Enabled: false };
        btnToggle.Text = ctxToggle.Text = enabling ? "Enable" : "Disable";
        btnToggle.Image = ctxToggle.Image = enabling ? _enableGlyph : _disableGlyph;

        int total = _config.VHosts.Count;
        int active = _config.VHosts.Count(x => x.Enabled);
        var unreachable = _config.VHosts
            .Where(x => x.Enabled && !HostsFile.PointsHere(x.ServerName, _loopbackNames))
            .Select(x => x.ServerName)
            .ToList();
        lblSummary.Text = total == 0
            ? "No virtual hosts yet. Add one to serve a project under its own name."
            : $"{total} virtual host{(total == 1 ? "" : "s")} · {active} active" +
              (unreachable.Count == 0
                  ? ""
                  : $" · add to hosts file: 127.0.0.1  {string.Join(" ", unreachable.Take(3))}{(unreachable.Count > 3 ? " …" : "")}");
        lblSummary.ForeColor = unreachable.Count == 0 ? SystemColors.GrayText : UiTheme.StatusTransition;
    }

    // ─── Quick actions ────────────────────────────────────────────────────────

    private void btnToggle_Click(object sender, EventArgs e)
    {
        if (Selected is not { } v) return;
        v.Enabled = !v.Enabled;
        ConfigChanged = true;
        LoadVHosts();
        Reselect(v);
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        if (Selected is not { Enabled: true } v) return;
        int port = v.Port > 0 ? v.Port : _config.Apache.Port;
        bool https = (_config.Apache.EnableSsl && port == _config.Apache.SslPort) ||
                     (_config.Apache.ExtraPorts ?? []).Any(p => p.Enabled && p.IsSsl && p.Port == port);
        string scheme = https ? "https" : "http";
        bool defaultPort = (!https && port == 80) || (https && port == 443);
        string url = $"{scheme}://{v.ServerName}{(defaultPort ? "" : $":{port}")}/";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open {url}:\n{ex.Message}", "Hamster Server",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnOpenFolder_Click(object sender, EventArgs e)
    {
        if (Selected is not { } v || !Directory.Exists(v.DocumentRoot)) return;
        Process.Start("explorer.exe", $"\"{v.DocumentRoot}\"");
    }

    private void Reselect(VHostEntry v)
    {
        var item = lvVHosts.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == v);
        if (item is null) return;
        item.Selected = true;
        item.Focused = true;
        item.EnsureVisible();
    }

    // ─── Toolbar events ───────────────────────────────────────────────────────

    private void btnAdd_Click(object sender, EventArgs e)
    {
        using var dlg = new VHostDialog(_config, _rootDir, null);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) return;

        _config.VHosts.Add(dlg.Result);
        LoadVHosts();
        ConfigChanged = true;
        NeedsPhpRestart |= dlg.PhpConfigChanged;

        // Select the new item
        var newItem = lvVHosts.Items.Cast<ListViewItem>()
            .FirstOrDefault(i => i.Tag == dlg.Result);
        if (newItem != null) { newItem.Selected = true; newItem.EnsureVisible(); }
    }

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (lvVHosts.SelectedItems.Count == 0) return;
        var v = (VHostEntry)lvVHosts.SelectedItems[0].Tag!;

        using var dlg = new VHostDialog(_config, _rootDir, v);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) return;

        LoadVHosts();
        ConfigChanged = true;
        NeedsPhpRestart |= dlg.PhpConfigChanged;

        var edited = lvVHosts.Items.Cast<ListViewItem>()
            .FirstOrDefault(i => i.Tag == v);
        if (edited != null) { edited.Selected = true; edited.EnsureVisible(); }
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (lvVHosts.SelectedItems.Count == 0) return;
        var v = (VHostEntry)lvVHosts.SelectedItems[0].Tag!;

        if (MessageBox.Show($"Delete VHost \"{v.ServerName}\"?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _config.VHosts.Remove(v);
        LoadVHosts();
        ConfigChanged = true;
    }

    // ─── Selection / keyboard ─────────────────────────────────────────────────

    private void lvVHosts_DoubleClick(object sender, EventArgs e) => btnEdit_Click(sender, e);

    private void lvVHosts_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtons();

    private void lvVHosts_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete) btnDelete_Click(sender, e);
        if (e.KeyCode == Keys.Return) btnEdit_Click(sender, e);
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();
}
