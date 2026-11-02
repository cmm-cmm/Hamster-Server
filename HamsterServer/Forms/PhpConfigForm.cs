using System.Runtime.InteropServices;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

/// <summary>
/// Quick PHP settings for the selected runtime, opened from the Services list.
/// <para>
/// Every value here is stored in <see cref="AppConfig"/> and php.ini is regenerated from it — the
/// dialog never patches php.ini text. It used to, and that is why extension changes "did not
/// stick": <see cref="ServerManager.SynchronizePhpIni"/> force-regenerates every runtime's php.ini
/// from AppConfig after a settings save, a PHP version switch, a vhost change or a runtime install,
/// so whatever this dialog had written into the file was silently replaced by the old selection.
/// </para>
/// </summary>
public partial class PhpConfigForm : Form
{
    private readonly AppConfig _config;
    private readonly PhpVersionInfo _phpInfo;
    private readonly Func<bool, Task> _applyAsync;
    private readonly string _phpIniPath;

    // The global selection, including names whose DLL this runtime lacks: one list serves every
    // installed runtime, so an extension missing here must survive a save made from this dialog.
    private readonly HashSet<string> _enabledExts;
    private IReadOnlyList<string> _availableExts = [];
    private bool _busy;

    public bool ConfigChanged { get; private set; }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_SETREDRAW = 0x000B;

    /// <param name="applyAsync">
    /// Persists the configuration and regenerates php.ini; the argument says whether a running PHP
    /// should be restarted. The main form owns saving and restarting, as it does for every dialog.
    /// </param>
    public PhpConfigForm(AppConfig config, PhpVersionInfo phpInfo, Func<bool, Task> applyAsync)
    {
        InitializeComponent();
        _config = config;
        _phpInfo = phpInfo;
        _applyAsync = applyAsync;
        _phpIniPath = Path.Combine(phpInfo.DirectoryPath, "php.ini");
        _enabledExts = new HashSet<string>(config.Php.EnabledExtensions ?? [], StringComparer.OrdinalIgnoreCase);

        Text = $"PHP {phpInfo.Version} Configuration";
        lblPhpTitle.Text = $"PHP {phpInfo.Version} — {_phpIniPath}";
        rtbPhpIni.Font = UiTheme.CreateMonospaceFont();

        // Only the action button gets a glyph — Save and Close are plain dialog buttons.
        // Amber, because this one bounces a running service.
        UiTheme.ApplyGlyph(btnPhpSaveRestart, UiTheme.Glyph.Refresh, UiTheme.StatusTransition);

        // display_errors = On corrupts FastCGI responses (output before headers), so the generated
        // php.ini always writes Off. The row stays visible to say so instead of offering a switch.
        chkDisplayErrors.Checked = false;
        chkDisplayErrors.Enabled = false;

        // InitializeComponent always creates `components`; the field is only declared nullable.
        _tips = new ToolTip(components!) { AutoPopDelay = 8000, InitialDelay = 400 };
        InitExtensionToolbar();
        UiTheme.ApplyIconScaling(tsExtensions, tsIni);

        cboTimezone.BeginUpdate();
        cboTimezone.Items.AddRange([.. TimezoneCatalog.Build(config.Php.Timezone)]);
        cboTimezone.EndUpdate();

        cboTimezone.TextChanged += (_, _) => ValidateFields();
        nudUploadMax.ValueChanged += (_, _) => ValidateFields();
        nudPostMax.ValueChanged += (_, _) => ValidateFields();
        nudMemoryLimit.ValueChanged += (_, _) => ValidateFields();
        // A DPI change rescales the font; the column width is measured from it, so re-measure.
        clbExtensions.FontChanged += (_, _) => LayoutExtensionColumns();

        LoadFromConfig();
        LoadExtensions();
        LoadRawIni();
        ValidateFields();
    }

    // Owned by `components`, so the designer's Dispose takes it with the form.
    private ToolTip? _tips;
    private readonly ToolStripLabel _extInfo = new() { ForeColor = SystemColors.GrayText };

    /// <summary>
    /// The extension actions moved here from Settings when the two dialogs were merged: this is now
    /// the only place the list is edited. Glyphs and colours are the ones Settings used.
    /// </summary>
    private void InitExtensionToolbar()
    {
        var recommended = new ToolStripButton("Recommended", UiTheme.CreateGlyph(UiTheme.Glyph.Star, UiTheme.AccentAction),
            (_, _) => ApplyRecommendedExtensions())
        {
            ToolTipText = "Select the Hamster Server / XAMPP-compatible profile, verified to load in every bundled PHP build."
        };
        var checkAll = new ToolStripButton("Check all", UiTheme.CreateGlyph(UiTheme.Glyph.SelectAll, UiTheme.AccentAction),
            (_, _) => SetVisibleExtensions(true))
        {
            ToolTipText = "Enable every extension shown (respects the filter). A broken DLL can stop PHP from starting."
        };
        var uncheckAll = new ToolStripButton("Uncheck all", UiTheme.CreateGlyph(UiTheme.Glyph.ClearSelection, UiTheme.AccentNeutral),
            (_, _) => SetVisibleExtensions(false))
        {
            ToolTipText = "Disable every extension shown (respects the filter)."
        };
        foreach (var button in new[] { recommended, checkAll, uncheckAll })
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

        tsExtensions.Items.Add(new ToolStripSeparator());
        tsExtensions.Items.AddRange(new ToolStripItem[] { recommended, checkAll, uncheckAll });
        tsExtensions.Items.Add(new ToolStripSeparator());
        tsExtensions.Items.Add(_extInfo);
    }


    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Closing mid-restart would dispose the controls the continuation is about to touch.
        if (_busy) { e.Cancel = true; return; }
        base.OnFormClosing(e);
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    private void LoadFromConfig()
    {
        var php = _config.Php;
        SetClamped(nudMemoryLimit, php.MemoryLimit);
        SetClamped(nudUploadMax, php.UploadMaxFilesize);
        SetClamped(nudPostMax, php.PostMaxSize);
        SetClamped(nudMaxExecTime, php.MaxExecutionTime);
        chkShortOpenTag.Checked = php.ShortOpenTag;
        cboTimezone.Text = php.Timezone;

        int index = (int)php.ErrorReporting;
        cmbErrorReporting.SelectedIndex = index >= 0 && index < cmbErrorReporting.Items.Count ? index : 0;
    }

    private static void SetClamped(NumericUpDown nud, int value) =>
        nud.Value = Math.Clamp(value, (int)nud.Minimum, (int)nud.Maximum);

    private void LoadExtensions()
    {
        // Same discovery the php.ini writer uses: php_*.dll in this runtime's ext/, minus OPcache,
        // which is a zend_extension and has its own switch in Settings. Listing it here once let a
        // user write `extension = opcache`, which PHP rejects at startup.
        _availableExts = PhpExtensionManager.GetAvailableExtensions(_phpInfo.DirectoryPath);
        FillExtensionList(txtExtFilter.Text.Trim());
    }

    private void FillExtensionList(string filter)
    {
        clbExtensions.BeginUpdate();
        try
        {
            clbExtensions.Items.Clear();
            foreach (var ext in _availableExts)
            {
                if (filter.Length == 0 || ext.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    clbExtensions.Items.Add(ext, _enabledExts.Contains(ext));
            }
        }
        finally { clbExtensions.EndUpdate(); }
        LayoutExtensionColumns();
        UpdateExtInfo();
    }

    /// <summary>
    /// Sizes the columns to the widest name at the current font. <see cref="CheckedListBox.ColumnWidth"/>
    /// is raw pixels and does not scale, and a MultiColumn list cannot be scrolled sideways with the
    /// wheel — a clipped name past the right edge is effectively unreachable.
    /// </summary>
    private void LayoutExtensionColumns()
    {
        int widest = 0;
        foreach (var ext in _availableExts)
            widest = Math.Max(widest, TextRenderer.MeasureText(ext, clbExtensions.Font).Width);
        if (widest > 0)
            clbExtensions.ColumnWidth = widest + clbExtensions.ItemHeight + 12;
    }

    private void UpdateExtInfo()
    {
        if (_availableExts.Count == 0)
        {
            _extInfo.Text = "No ext/ folder in this runtime";
            return;
        }
        var available = _availableExts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        int enabledHere = _enabledExts.Count(available.Contains);
        int elsewhere = _enabledExts.Count(name => !available.Contains(name));
        _extInfo.Text = elsewhere > 0
            ? $"{enabledHere} of {_availableExts.Count} enabled · {elsewhere} kept for other runtimes"
            : $"{enabledHere} of {_availableExts.Count} enabled · shared by every runtime";
    }

    // The profile is verified to load in every bundled build, so it replaces the whole selection —
    // including names this runtime lacks — rather than only the rows on screen.
    private void ApplyRecommendedExtensions()
    {
        _enabledExts.Clear();
        _enabledExts.UnionWith(PhpExtensionManager.RecommendedExtensions);
        FillExtensionList(txtExtFilter.Text.Trim());
    }

    // Acts on the rows the filter shows, so "filter: pdo → Check all" means what it looks like.
    private void SetVisibleExtensions(bool enabled)
    {
        foreach (string ext in clbExtensions.Items)
        {
            if (enabled) _enabledExts.Add(ext);
            else _enabledExts.Remove(ext);
        }
        FillExtensionList(txtExtFilter.Text.Trim());
    }

    private void LoadRawIni()
    {
        try
        {
            if (!File.Exists(_phpIniPath))
            {
                rtbPhpIni.Text = "(php.ini not found — save here or start PHP once to generate it)";
                return;
            }
            rtbPhpIni.Text = File.ReadAllText(_phpIniPath);
            ApplySyntaxHighlight(rtbPhpIni);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            rtbPhpIni.Text = $"(php.ini could not be read: {ex.Message})";
        }
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    private bool ValidateFields()
    {
        // Same character rule ConfigManager.Normalize enforces; checking it here means an invalid
        // zone is refused with a visible reason instead of silently reset to the default on save.
        bool timezoneOk = TimezoneCatalog.IsValid(cboTimezone.Text);
        cboTimezone.BackColor = timezoneOk ? UiTheme.FieldNormal : UiTheme.FieldError;

        // PHP checks post_max_size first, so an upload bigger than it never reaches
        // upload_max_filesize — the request arrives with empty $_POST and $_FILES instead.
        bool postTooSmall = nudPostMax.Value < nudUploadMax.Value;
        nudPostMax.BackColor = postTooSmall ? UiTheme.FieldWarning : UiTheme.FieldNormal;
        _tips?.SetToolTip(nudPostMax, postTooSmall
            ? "Smaller than the upload limit: an upload above this arrives with empty $_POST and $_FILES."
            : "post_max_size — must be at least the upload limit.");

        // Moved here from Settings with memory_limit itself: every worker may use this much, so the
        // worst case is workers × memory_limit, measured against the 70 % ServerManager warns at.
        long totalRamMb = (long)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024);
        long projectedMb = (long)_config.Php.Workers * (long)nudMemoryLimit.Value;
        bool overcommitted = totalRamMb > 0 && projectedMb > totalRamMb * 0.70;
        nudMemoryLimit.BackColor = overcommitted ? UiTheme.FieldWarning : UiTheme.FieldNormal;
        _tips?.SetToolTip(nudMemoryLimit, overcommitted
            ? $"{_config.Php.Workers} workers × {nudMemoryLimit.Value} MB = {projectedMb} MB worst case, " +
              $"against ~{totalRamMb} MB available. Lower this or the worker count in Settings."
            : "memory_limit — the most one PHP request may allocate.");

        btnPhpSave.Enabled = btnPhpSaveRestart.Enabled = timezoneOk && !_busy;
        return timezoneOk;
    }

    // ─── Syntax highlight ─────────────────────────────────────────────────────

    private void ApplySyntaxHighlight(RichTextBox rtb)
    {
        SendMessage(rtb.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            int savedSel = rtb.SelectionStart;
            rtb.SelectAll();
            rtb.SelectionColor = UiTheme.SyntaxDefault;

            var text = rtb.Text;
            int pos = 0;
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(';'))
                {
                    ColorSpan(rtb, pos, line.Length, UiTheme.SyntaxComment);
                }
                else if (trimmed.StartsWith('['))
                {
                    ColorSpan(rtb, pos, line.Length, UiTheme.SyntaxSection);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    int eqIdx = line.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        ColorSpan(rtb, pos, eqIdx, UiTheme.SyntaxKeyword);
                        ColorSpan(rtb, pos + eqIdx, line.Length - eqIdx, UiTheme.SyntaxValue);

                        // Quoted paths, then bare On/Off and numeric values, get their own colour
                        var value = line[(eqIdx + 1)..];
                        int valueStart = pos + eqIdx + 1;
                        if (value.Contains('"'))
                        {
                            int open = value.IndexOf('"');
                            int close = value.LastIndexOf('"');
                            if (close > open)
                                ColorSpan(rtb, valueStart + open, close - open + 1, UiTheme.SyntaxString);
                        }
                        else
                        {
                            var literal = value.Trim();
                            if (literal.Length > 0 &&
                                (char.IsDigit(literal[0]) ||
                                 literal.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                                 literal.Equals("Off", StringComparison.OrdinalIgnoreCase)))
                            {
                                int offset = value.IndexOf(literal[0]);
                                ColorSpan(rtb, valueStart + offset, literal.Length, UiTheme.SyntaxNumber);
                            }
                        }
                    }
                }
                pos += line.Length + 1;
            }

            rtb.SelectionStart = savedSel;
            rtb.SelectionLength = 0;
        }
        finally
        {
            SendMessage(rtb.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            rtb.Invalidate();
        }
    }

    private static void ColorSpan(RichTextBox rtb, int start, int length, Color color)
    {
        if (start < 0 || length <= 0 || start + length > rtb.TextLength) return;
        rtb.SelectionStart = start;
        rtb.SelectionLength = length;
        rtb.SelectionColor = color;
    }

    // ─── Extension filter ─────────────────────────────────────────────────────

    private void txtExtFilter_TextChanged(object sender, EventArgs e) =>
        FillExtensionList(txtExtFilter.Text.Trim());

    // ItemCheck also fires while FillExtensionList re-adds checked rows; adding a name that is
    // already in the set is a no-op, so the filter never changes the selection.
    private void clbExtensions_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        var ext = clbExtensions.Items[e.Index]?.ToString();
        if (string.IsNullOrEmpty(ext)) return;
        if (e.NewValue == CheckState.Checked) _enabledExts.Add(ext);
        else _enabledExts.Remove(ext);
        UpdateExtInfo();
    }

    // ─── Reload raw ───────────────────────────────────────────────────────────

    private void btnReloadIni_Click(object sender, EventArgs e) => LoadRawIni();

    // ─── Save ─────────────────────────────────────────────────────────────────

    private async void btnPhpSaveRestart_Click(object sender, EventArgs e) => await SaveAsync(restart: true);

    private async void btnPhpSave_Click(object sender, EventArgs e) => await SaveAsync(restart: false);

    private void btnPhpCancel_Click(object sender, EventArgs e) => Close();

    private async Task SaveAsync(bool restart)
    {
        if (_busy || !ValidateFields()) return;

        bool changed = ApplyToConfig();
        if (!changed && !restart)
        {
            LoadRawIni();
            return;
        }

        _busy = true;
        ValidateFields();
        UseWaitCursor = true;
        try
        {
            ConfigChanged |= changed;
            await _applyAsync(restart);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not apply PHP settings:\n{ex.Message}", "Hamster Server",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
            {
                UseWaitCursor = false;
                ValidateFields();
                LoadRawIni();
            }
        }
    }

    /// <summary>Copies the dialog into <see cref="AppConfig"/>; returns whether anything changed.</summary>
    private bool ApplyToConfig()
    {
        var php = _config.Php;
        var extensions = _enabledExts
            .Where(name => !name.Equals(PhpExtensionManager.Opcache, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var errorReporting = (PhpErrorReporting)Math.Max(0, cmbErrorReporting.SelectedIndex);
        var timezone = cboTimezone.Text.Trim();

        bool changed =
            php.MemoryLimit != (int)nudMemoryLimit.Value ||
            php.UploadMaxFilesize != (int)nudUploadMax.Value ||
            php.PostMaxSize != (int)nudPostMax.Value ||
            php.MaxExecutionTime != (int)nudMaxExecTime.Value ||
            php.ShortOpenTag != chkShortOpenTag.Checked ||
            php.ErrorReporting != errorReporting ||
            !string.Equals(php.Timezone, timezone, StringComparison.Ordinal) ||
            !(php.EnabledExtensions ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(extensions);

        if (!changed) return false;

        php.MemoryLimit = (int)nudMemoryLimit.Value;
        php.UploadMaxFilesize = (int)nudUploadMax.Value;
        php.PostMaxSize = (int)nudPostMax.Value;
        php.MaxExecutionTime = (int)nudMaxExecTime.Value;
        php.ShortOpenTag = chkShortOpenTag.Checked;
        php.ErrorReporting = errorReporting;
        php.Timezone = timezone;
        php.EnabledExtensions = extensions;
        return true;
    }
}
