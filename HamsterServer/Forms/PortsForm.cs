using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class PortsForm : Form
{
    private readonly AppConfig _config;
    public bool ConfigChanged { get; private set; }

    private const string TagHttpPrimary = "http-primary";
    private const string TagHttpsPrimary = "https-primary";

    // Indices into _portIcons, in the order they are added below.
    private const int IconHttp = 0;
    private const int IconHttps = 1;
    private const int IconHttpOff = 2;
    private const int IconHttpsOff = 3;

    private ImageList _portIcons = null!;

    public PortsForm(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        InitIcons();
        LoadPorts();
        UpdateHint();

        var tt = new ToolTip { AutoPopDelay = 5000, InitialDelay = 400 };
        tt.SetToolTip(lvPorts, "Check/uncheck to enable or disable a port.  Double-click to edit.");
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Resize does not fire when the size never changed — size the last column explicitly.
        lvPorts_Resize(this, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Icons ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A closed padlock means the port terminates TLS, an open one means plain HTTP; grey means
    /// the port is switched off. The Kind and Status columns still spell it out in words.
    /// </summary>
    private void InitIcons()
    {
        _portIcons = new ImageList(components) { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Unlock, UiTheme.AccentHttp));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Lock, UiTheme.AccentHttps));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Unlock, UiTheme.StatusStopped));
        _portIcons.Images.Add(UiTheme.CreateGlyph(UiTheme.Glyph.Lock, UiTheme.StatusStopped));
        lvPorts.SmallImageList = _portIcons;

        btnAdd.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Add, UiTheme.StatusRunning);
        btnEdit.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Edit, UiTheme.AccentAction);
        btnRemove.Image = UiTheme.CreateGlyph(UiTheme.Glyph.Delete, UiTheme.StatusError);
        foreach (var b in new[] { btnAdd, btnEdit, btnRemove })
            b.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
    }

    private static int PortIcon(bool enabled, bool isSsl) =>
        isSsl ? (enabled ? IconHttps : IconHttpsOff)
              : (enabled ? IconHttp : IconHttpOff);

    // ─── Load ─────────────────────────────────────────────────────────────────

    private void LoadPorts()
    {
        lvPorts.ItemChecked -= lvPorts_ItemChecked;
        try
        {
            lvPorts.Items.Clear();

            var httpItem = new ListViewItem(_config.Apache.Port.ToString());
            httpItem.SubItems.Add("—");
            httpItem.SubItems.Add("Primary HTTP");
            httpItem.SubItems.Add("Active");
            httpItem.Tag = TagHttpPrimary;
            httpItem.Checked = true;
            httpItem.ForeColor = SystemColors.WindowText;
            httpItem.ImageIndex = IconHttp;
            lvPorts.Items.Add(httpItem);

            bool sslOn = _config.Apache.EnableSsl;
            var httpsItem = new ListViewItem(_config.Apache.SslPort.ToString());
            httpsItem.SubItems.Add("✓");
            httpsItem.SubItems.Add("Primary HTTPS");
            httpsItem.SubItems.Add(sslOn ? "Active" : "Disabled");
            httpsItem.Tag = TagHttpsPrimary;
            httpsItem.Checked = sslOn;
            httpsItem.ForeColor = sslOn ? UiTheme.StatusRunning : SystemColors.GrayText;
            httpsItem.ImageIndex = PortIcon(sslOn, isSsl: true);
            lvPorts.Items.Add(httpsItem);

            foreach (var p in _config.Apache.ExtraPorts ?? [])
            {
                var item = new ListViewItem(p.Port.ToString());
                item.SubItems.Add(p.IsSsl ? "✓" : "—");
                item.SubItems.Add(p.IsSsl ? "Extra HTTPS" : "Extra HTTP");
                item.SubItems.Add(p.Enabled ? "Active" : "Disabled");
                item.Tag = p;
                item.Checked = p.Enabled;
                item.ForeColor = GetPortColor(p.Enabled, p.IsSsl);
                item.ImageIndex = PortIcon(p.Enabled, p.IsSsl);
                lvPorts.Items.Add(item);
            }
        }
        finally
        {
            lvPorts.ItemChecked += lvPorts_ItemChecked;
            UpdateButtons();
        }
    }

    private static Color GetPortColor(bool enabled, bool isSsl)
    {
        if (!enabled) return SystemColors.GrayText;
        return isSsl ? UiTheme.StatusRunning : SystemColors.WindowText;
    }

    private void UpdateHint()
    {
        var parts = new List<string> { $":{_config.Apache.Port}" };
        if (_config.Apache.EnableSsl)
            parts.Add($":{_config.Apache.SslPort} (SSL)");
        foreach (var ep in (_config.Apache.ExtraPorts ?? []).Where(p => p.Enabled))
            parts.Add($":{ep.Port}{(ep.IsSsl ? " (SSL)" : "")}");
        lblHint.Text = "Apache will listen on:  " + string.Join("   ·   ", parts);
    }

    private void UpdateButtons()
    {
        bool hasSelection = lvPorts.SelectedItems.Count > 0;
        btnEdit.Enabled = ctxEdit.Enabled = hasSelection;
        btnRemove.Enabled = ctxRemove.Enabled =
            hasSelection && lvPorts.SelectedItems[0].Tag is PortEntry;
    }

    // ─── ItemChecked ──────────────────────────────────────────────────────────

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
                e.Item.SubItems[3].Text = e.Item.Checked ? "Active" : "Disabled";
                e.Item.ForeColor = e.Item.Checked ? UiTheme.StatusRunning : SystemColors.GrayText;
                e.Item.ImageIndex = PortIcon(e.Item.Checked, isSsl: true);
                UpdateHint();
                ConfigChanged = true;
                return;

            case PortEntry entry:
                entry.Enabled = e.Item.Checked;
                e.Item.SubItems[3].Text = entry.Enabled ? "Active" : "Disabled";
                e.Item.ForeColor = GetPortColor(entry.Enabled, entry.IsSsl);
                e.Item.ImageIndex = PortIcon(entry.Enabled, entry.IsSsl);
                UpdateHint();
                ConfigChanged = true;
                return;
        }
    }

    // ─── Add ──────────────────────────────────────────────────────────────────

    private void btnAdd_Click(object sender, EventArgs e)
    {
        using var dlg = PortDialog.ForAdd(_config);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        (_config.Apache.ExtraPorts ??= []).Add(new PortEntry
        {
            Port = dlg.PortValue,
            IsSsl = dlg.IsSsl,
            Enabled = true
        });
        LoadPorts();
        UpdateHint();
        ConfigChanged = true;

        if (lvPorts.Items.Count > 0)
            lvPorts.Items[^1].Selected = true;
    }

    // ─── Edit ─────────────────────────────────────────────────────────────────

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (lvPorts.SelectedItems.Count == 0) return;
        var item = lvPorts.SelectedItems[0];

        PortDialog dlg;
        switch (item.Tag)
        {
            case TagHttpPrimary:
                dlg = PortDialog.ForEditPrimaryHttp(_config);
                break;
            case TagHttpsPrimary:
                dlg = PortDialog.ForEditPrimaryHttps(_config);
                break;
            case PortEntry entry:
                dlg = PortDialog.ForEditExtra(_config, entry);
                break;
            default:
                return;
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

        LoadPorts();
        UpdateHint();
        ConfigChanged = true;
    }

    // ─── Remove ───────────────────────────────────────────────────────────────

    private void btnRemove_Click(object sender, EventArgs e)
    {
        if (lvPorts.SelectedItems.Count == 0) return;
        var item = lvPorts.SelectedItems[0];

        if (item.Tag is not PortEntry entry)
        {
            MessageBox.Show("Primary ports cannot be removed.\nUse the checkbox to disable them.",
                "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        (_config.Apache.ExtraPorts ?? []).Remove(entry);
        LoadPorts();
        UpdateHint();
        ConfigChanged = true;
    }

    // ─── Selection / keyboard ─────────────────────────────────────────────────

    private void lvPorts_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtons();

    // Let the last column absorb the leftover width so the grid has no dead strip on the right.
    private void lvPorts_Resize(object sender, EventArgs e)
    {
        int used = colPort.Width + colSsl.Width + colKind.Width;
        colStatus.Width = Math.Max(120, lvPorts.ClientSize.Width - used);
    }

    private void lvPorts_DoubleClick(object sender, EventArgs e) => btnEdit_Click(sender, e);

    private void lvPorts_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete) btnRemove_Click(sender, e);
        if (e.KeyCode == Keys.Return) btnEdit_Click(sender, e);
    }

    // ─── Close ────────────────────────────────────────────────────────────────

    private void btnClose_Click(object sender, EventArgs e) => Close();
}
