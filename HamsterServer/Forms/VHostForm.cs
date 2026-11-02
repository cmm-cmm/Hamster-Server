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
        foreach (var b in new[] { btnAdd, btnEdit, btnDelete })
            b.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

        // The right-click menu repeats the toolbar's Edit / Delete, so it carries the same glyphs.
        ctxEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        ctxDelete.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        UiTheme.ApplyIconScaling(toolStrip, ctxVHosts);
    }

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
        int total = lvVHosts.ClientSize.Width - 4;
        int flex = total - 130 - 60 - 150 - 80;  // Alias + Port + Framework + Status
        if (flex < 80) return;
        colServerName.Width = (int)(flex * 0.38);
        colAlias.Width = 130;
        colPort.Width = 60;
        colFramework.Width = 150;
        colDocRoot.Width = (int)(flex * 0.62);
        colEnabled.Width = 80;
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
        item.SubItems.Add(v.Enabled ? "Active" : "Disabled");
        item.Tag = v;
        item.ForeColor = v.Enabled ? SystemColors.WindowText : SystemColors.GrayText;
        // Same vocabulary as the Services list: green is serving, grey is switched off.
        item.SubItems[5].ForeColor = v.Enabled ? UiTheme.StatusRunning : UiTheme.StatusStopped;
        item.ImageIndex = !v.Enabled ? IconDisabled
                        : profile.IsReverseProxy ? IconProxy
                        : IconEnabled;
        return item;
    }

    private void UpdateButtons()
    {
        bool has = lvVHosts.SelectedItems.Count > 0;
        btnEdit.Enabled = ctxEdit.Enabled = has;
        btnDelete.Enabled = ctxDelete.Enabled = has;
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
