using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly string _rootDir;
    public bool ConfigChanged { get; private set; }
    public bool NeedsApacheRestart { get; private set; }
    public bool NeedsPhpRestart { get; private set; }

    public SettingsForm(AppConfig config, string rootDir)
    {
        InitializeComponent();
        cboServerMode.DataSource = Enum.GetValues<ServerMode>();
        _config = config;
        _rootDir = rootDir;
        PopulateTimezones();
        LoadSettings();
        chkSsl.CheckedChanged += (_, _) => UpdateSslCertVisibility();
        chkOpcache.CheckedChanged += (_, _) => UpdateOpcacheVisibility();
        chkAllowLanAccess.CheckedChanged += (_, _) => UpdateRiskHighlights();
        nudWorkers.ValueChanged += (_, _) => UpdateRiskHighlights();
        nudMemoryLimit.ValueChanged += (_, _) => UpdateRiskHighlights();
        // A DPI change rescales the font; the column width is measured from it, so re-measure.
        clbExtensions.FontChanged += (_, _) => LayoutExtensionColumns();
        UpdateSslCertVisibility();
        UpdateOpcacheVisibility();
        UpdateRiskHighlights();
        InitTooltips();
    }

    /// <summary>
    /// Flags the two settings that can bite silently: exposing the server to the LAN, and a
    /// worker count whose worst-case memory exceeds what the machine has. Both stay editable —
    /// this only colours them so the consequence is visible before saving.
    /// </summary>
    private void UpdateRiskHighlights()
    {
        chkAllowLanAccess.ForeColor = chkAllowLanAccess.Checked
            ? UiTheme.StatusTransition
            : SystemColors.ControlText;

        long totalRamMb = (long)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024);
        long projectedMb = (long)nudWorkers.Value * (long)nudMemoryLimit.Value;
        bool overcommitted = totalRamMb > 0 && projectedMb > totalRamMb * 0.70;

        nudWorkers.BackColor = overcommitted ? UiTheme.FieldWarning : UiTheme.FieldNormal;
        nudMemoryLimit.BackColor = overcommitted ? UiTheme.FieldWarning : UiTheme.FieldNormal;

        lblMemoryWarning.Visible = overcommitted;
        if (overcommitted)
            lblMemoryWarning.Text =
                $"{nudWorkers.Value} workers × {nudMemoryLimit.Value} MB = {projectedMb} MB worst case, " +
                $"against ~{totalRamMb} MB available.";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { btnCancel_Click(this, EventArgs.Empty); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void InitTooltips()
    {
        var tt = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400 };
        tt.SetToolTip(nudKeepAlive, "Idle connection keep-alive timeout (seconds). Lower = less resource use.");
        tt.SetToolTip(nudMaxClients, "Maximum simultaneous connections Apache will handle.");
        tt.SetToolTip(nudOpcacheMemory, "Memory (MB) for PHP OPcache. 128 MB is sufficient for development.");
        tt.SetToolTip(chkOpcache, "Compile PHP to bytecode on first request — highly recommended.");
        tt.SetToolTip(chkGzip, "Compress text responses (HTML, CSS, JS) to reduce bandwidth.");
        tt.SetToolTip(chkCache, "Send Expires headers so browsers cache static assets locally.");
        tt.SetToolTip(chkBrotli, "Brotli compression (15-25% smaller than Gzip). Requires mod_brotli.so in apache/Apache24/modules/.");
        tt.SetToolTip(nudWorkers, "PHP-CGI worker processes. Start with CPU count × 2.");
        tt.SetToolTip(nudMaxRequests, "Workers restart after this many requests (prevents memory leaks).");
        tt.SetToolTip(cboTimezone, "PHP date.timezone using an IANA identifier.");
        tt.SetToolTip(chkUseBundledCaCertificate,
            $"Configure cURL and OpenSSL to use {Path.Combine(_rootDir, "ssl", "cacert.pem")} for HTTPS requests.");
        tt.SetToolTip(cboServerMode, "Development: verbose errors, no caching.  Production: optimized config.");
        tt.SetToolTip(btnApplyModeDefaults, "Pre-fill workers, memory, and timeouts with optimal values for the selected mode.");
        tt.SetToolTip(btnBrowseSslCert, "Browse for SSL certificate file (.crt / .pem)");
        tt.SetToolTip(btnBrowseSslKey, "Browse for SSL private key file (.key / .pem)");
        tt.SetToolTip(clbExtensions, "Tick to enable extension in php.ini. Scan reads php/{version}/ext/php_*.dll automatically.");
        tt.SetToolTip(btnRecommendedExtensions,
            "Select the recommended Hamster Server/XAMPP-compatible extensions available in this PHP version.");
        tt.SetToolTip(btnCheckAll, "Enable all found extensions (may cause startup errors if a DLL is corrupt).");
        tt.SetToolTip(btnUncheckAll, "Disable all extensions (not recommended — PHP will lose core functionality).");
        tt.SetToolTip(chkAllowLanAccess, "Off by default: Apache listens only on 127.0.0.1. Enable only on trusted networks.");
    }

    // ─── PHP Extensions ───────────────────────────────────────────────────────

    private void PopulateTimezones()
    {
        var timezones = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "UTC",
            "Asia/Ho_Chi_Minh"
        };

        try
        {
            foreach (var timezone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (timezone.Id.Contains('/'))
                    timezones.Add(timezone.Id);
                else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timezone.Id, out var ianaId))
                    timezones.Add(ianaId);
            }
        }
        catch (TimeZoneNotFoundException)
        {
            // Keep the safe defaults if the OS timezone registry is unavailable.
        }
        catch (InvalidTimeZoneException)
        {
            // Keep the safe defaults if the OS contains corrupt timezone data.
        }

        var configuredTimezone = string.IsNullOrWhiteSpace(_config.Php.Timezone)
            ? "Asia/Ho_Chi_Minh"
            : _config.Php.Timezone.Trim();
        timezones.Add(configuredTimezone);

        cboTimezone.BeginUpdate();
        cboTimezone.Items.Clear();
        cboTimezone.Items.Add("UTC");
        cboTimezone.Items.Add("Asia/Ho_Chi_Minh");
        foreach (var timezone in timezones.Where(timezone =>
                     !timezone.Equals("UTC", StringComparison.OrdinalIgnoreCase) &&
                     !timezone.Equals("Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase)))
        {
            cboTimezone.Items.Add(timezone);
        }
        cboTimezone.EndUpdate();
        SelectTimezone(configuredTimezone);
    }

    private void SelectTimezone(string timezone)
    {
        cboTimezone.SelectedItem = cboTimezone.Items.Cast<string>()
            .FirstOrDefault(item => item.Equals(timezone, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Scan php/{version}/ext/php_*.dll và populate clbExtensions.
    /// Những extension nằm trong EnabledExtensions được tick sẵn.
    /// Extension user đã enable nhưng không có DLL vẫn hiện (có thể là built-in).
    /// </summary>
    private void PopulateExtensions()
    {
        clbExtensions.Items.Clear();

        var version = _config.Php.CurrentVersion;
        var extDir = string.IsNullOrWhiteSpace(version)
            ? null
            : Path.Combine(_rootDir, "php", version, "ext");

        var enabled = new HashSet<string>(_config.Php.EnabledExtensions, StringComparer.OrdinalIgnoreCase);

        // Fix 4.2: bọc scan trong try-catch — tránh crash khi ext/ bị locked/mất quyền
        HashSet<string> fromDisk;
        try
        {
            fromDisk = extDir != null
                ? PhpExtensionManager.GetAvailableExtensions(Path.GetDirectoryName(extDir)!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            lblExtInfo.Text = $"Cannot read ext/ folder: {ex.Message}";
            // Vẫn hiển thị các extension đang enable từ config
            clbExtensions.BeginUpdate();
            foreach (var ext in enabled.OrderBy(e => e))
                clbExtensions.Items.Add(ext, true);
            clbExtensions.EndUpdate();
            LayoutExtensionColumns();
            return;
        }

        // Union: DLLs từ disk + extension đang được enable (để không mất ext user đã lưu)
        var allExts = fromDisk
            .Union(enabled, StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        clbExtensions.BeginUpdate();
        foreach (var ext in allExts)
            clbExtensions.Items.Add(ext, enabled.Contains(ext));
        clbExtensions.EndUpdate();

        LayoutExtensionColumns();
        UpdateExtInfo();
    }

    /// <summary>
    /// Sizes the extension columns to the widest name at the current font size. The designer
    /// carried a fixed 130 px, which wasted width at 100 % and clipped names once the display
    /// scaled — <see cref="CheckedListBox.ColumnWidth"/> is raw pixels and does not scale.
    /// </summary>
    private void LayoutExtensionColumns()
    {
        if (clbExtensions.Items.Count == 0) return;

        int widest = 0;
        foreach (string name in clbExtensions.Items)
            widest = Math.Max(widest, TextRenderer.MeasureText(name, clbExtensions.Font).Width);

        // The check box and the gap before the label both track the item height.
        clbExtensions.ColumnWidth = widest + clbExtensions.ItemHeight + 12;
    }

    private void UpdateExtInfo()
    {
        int total = clbExtensions.Items.Count;
        int checked_ = clbExtensions.CheckedItems.Count;
        lblExtInfo.Text = total > 0
            ? $"Found: {total} extensions   Enabled: {checked_}"
            : "No PHP version selected or ext/ folder not found.";
    }

    private List<string> GetCheckedExtensions() =>
        clbExtensions.CheckedItems.Cast<string>().ToList();

    private void clbExtensions_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // ItemCheck fires before the collection updates. During constructor population the
        // window handle does not exist yet, and PopulateExtensions updates the label itself.
        if (IsHandleCreated && !IsDisposed)
            BeginInvoke(UpdateExtInfo);
    }

    private void btnCheckAll_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < clbExtensions.Items.Count; i++)
            clbExtensions.SetItemChecked(i, true);
        UpdateExtInfo();
    }

    private void btnRecommendedExtensions_Click(object sender, EventArgs e)
    {
        var recommended = PhpExtensionManager.RecommendedExtensions
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < clbExtensions.Items.Count; i++)
        {
            var extension = clbExtensions.Items[i]?.ToString() ?? "";
            clbExtensions.SetItemChecked(i, recommended.Contains(extension));
        }
        UpdateExtInfo();
    }

    private void btnUncheckAll_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < clbExtensions.Items.Count; i++)
            clbExtensions.SetItemChecked(i, false);
        UpdateExtInfo();
    }

    private void UpdateOpcacheVisibility()
    {
        bool show = chkOpcache.Checked;
        lblCapOpcacheMem.Visible = show;
        nudOpcacheMemory.Visible = show;
    }

    private void btnBrowseSslCert_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Certificate files (*.crt;*.pem)|*.crt;*.pem|All files (*.*)|*.*",
            Title = "Select SSL Certificate File"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            txtSslCertFile.Text = dlg.FileName;
    }

    private void btnBrowseSslKey_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Key files (*.key;*.pem)|*.key;*.pem|All files (*.*)|*.*",
            Title = "Select SSL Key File"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            txtSslKeyFile.Text = dlg.FileName;
    }

    private void UpdateSslCertVisibility()
    {
        bool show = chkSsl.Checked;
        lblCapSslCert.Visible = show;
        txtSslCertFile.Visible = show;
        btnBrowseSslCert.Visible = show;
        lblCapSslKey.Visible = show;
        txtSslKeyFile.Visible = show;
        btnBrowseSslKey.Visible = show;
    }

    private void LoadSettings()
    {
        // Mode
        cboServerMode.SelectedItem = _config.ServerMode;

        // Apache
        txtServerName.Text = _config.Apache.ServerName;
        nudMaxClients.Value = _config.Apache.MaxClients;
        nudKeepAlive.Value = _config.Apache.KeepAliveTimeout;
        chkGzip.Checked = _config.Apache.EnableGzip;
        chkCache.Checked = _config.Apache.EnableCache;
        chkBrotli.Checked = _config.Apache.EnableBrotli;
        chkAllowLanAccess.Checked = _config.Apache.AllowLanAccess;
        chkSsl.Checked = _config.Apache.EnableSsl;
        txtSslCertFile.Text = _config.Apache.SslCertFile;
        txtSslKeyFile.Text = _config.Apache.SslKeyFile;

        // PHP
        nudFcgiPort.Value = _config.Php.FcgiPort;
        nudWorkers.Value = _config.Php.Workers;
        nudMaxRequests.Value = _config.Php.MaxRequests;
        nudMemoryLimit.Value = _config.Php.MemoryLimit;
        nudMaxExecTime.Value = _config.Php.MaxExecutionTime;
        chkOpcache.Checked = _config.Php.EnableOpcache;
        nudOpcacheMemory.Value = _config.Php.OpcacheMemory;
        SelectTimezone(_config.Php.Timezone);
        chkUseBundledCaCertificate.Checked = _config.Php.UseBundledCaCertificate;

        // General
        chkMinimizeToTray.Checked = _config.MinimizeToTray;
        chkAutoStart.Checked = _config.AutoStart;
        chkStartWithWindows.Checked = _config.StartWithWindows;

        // PHP Extensions — scan ext/ folder và tick những ext đang được enable
        PopulateExtensions();
    }

    private void SaveSettings()
    {
        string newTimezone = cboTimezone.SelectedItem?.ToString() ?? "Asia/Ho_Chi_Minh";

        // Detect what actually changed before writing (config still holds old values here)
        NeedsApacheRestart =
            _config.ServerMode != (ServerMode)cboServerMode.SelectedItem! ||
            !string.Equals(_config.Apache.ServerName, txtServerName.Text.Trim(), StringComparison.Ordinal) ||
            _config.Apache.MaxClients != (int)nudMaxClients.Value ||
            _config.Apache.KeepAliveTimeout != (int)nudKeepAlive.Value ||
            _config.Apache.EnableGzip != chkGzip.Checked ||
            _config.Apache.EnableCache != chkCache.Checked ||
            _config.Apache.EnableBrotli != chkBrotli.Checked ||
            _config.Apache.AllowLanAccess != chkAllowLanAccess.Checked ||
            _config.Apache.EnableSsl != chkSsl.Checked ||
            !string.Equals(_config.Apache.SslCertFile, txtSslCertFile.Text.Trim(), StringComparison.Ordinal) ||
            !string.Equals(_config.Apache.SslKeyFile, txtSslKeyFile.Text.Trim(), StringComparison.Ordinal);

        var newExtensions = GetCheckedExtensions();
        var oldExtSet = _config.Php.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newExtSet = newExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool extsChanged = !oldExtSet.SetEquals(newExtSet);

        NeedsPhpRestart =
            _config.Php.FcgiPort != (int)nudFcgiPort.Value ||
            _config.Php.Workers != (int)nudWorkers.Value ||
            _config.Php.MaxRequests != (int)nudMaxRequests.Value ||
            _config.Php.MemoryLimit != (int)nudMemoryLimit.Value ||
            _config.Php.MaxExecutionTime != (int)nudMaxExecTime.Value ||
            _config.Php.EnableOpcache != chkOpcache.Checked ||
            _config.Php.OpcacheMemory != (int)nudOpcacheMemory.Value ||
            !string.Equals(_config.Php.Timezone, newTimezone, StringComparison.Ordinal) ||
            _config.Php.UseBundledCaCertificate != chkUseBundledCaCertificate.Checked ||
            extsChanged;

        bool generalChanged =
            _config.MinimizeToTray != chkMinimizeToTray.Checked ||
            _config.AutoStart != chkAutoStart.Checked ||
            _config.StartWithWindows != chkStartWithWindows.Checked;

        ConfigChanged = NeedsApacheRestart || NeedsPhpRestart || generalChanged;

        // Write new values
        _config.ServerMode = (ServerMode)cboServerMode.SelectedItem!;

        _config.Apache.ServerName = txtServerName.Text.Trim();
        _config.Apache.MaxClients = (int)nudMaxClients.Value;
        _config.Apache.KeepAliveTimeout = (int)nudKeepAlive.Value;
        _config.Apache.EnableGzip = chkGzip.Checked;
        _config.Apache.EnableCache = chkCache.Checked;
        _config.Apache.EnableBrotli = chkBrotli.Checked;
        _config.Apache.AllowLanAccess = chkAllowLanAccess.Checked;
        _config.Apache.EnableSsl = chkSsl.Checked;
        _config.Apache.SslCertFile = txtSslCertFile.Text.Trim();
        _config.Apache.SslKeyFile = txtSslKeyFile.Text.Trim();

        _config.Php.FcgiPort = (int)nudFcgiPort.Value;
        _config.Php.Workers = (int)nudWorkers.Value;
        _config.Php.MaxRequests = (int)nudMaxRequests.Value;
        _config.Php.MemoryLimit = (int)nudMemoryLimit.Value;
        _config.Php.MaxExecutionTime = (int)nudMaxExecTime.Value;
        _config.Php.EnableOpcache = chkOpcache.Checked;
        _config.Php.OpcacheMemory = (int)nudOpcacheMemory.Value;
        _config.Php.Timezone = newTimezone;
        _config.Php.UseBundledCaCertificate = chkUseBundledCaCertificate.Checked;
        _config.Php.EnabledExtensions = newExtensions;

        _config.MinimizeToTray = chkMinimizeToTray.Checked;
        _config.AutoStart = chkAutoStart.Checked;
        _config.StartWithWindows = chkStartWithWindows.Checked;
    }

    private async void btnApplyModeDefaults_Click(object sender, EventArgs e)
    {
        bool isProd = (ServerMode)cboServerMode.SelectedItem! == ServerMode.Production;
        if (isProd)
        {
            nudWorkers.Value = Math.Max(8, Environment.ProcessorCount * 4);
            nudMaxRequests.Value = 10000;
            nudOpcacheMemory.Value = 256;
            nudKeepAlive.Value = 5;
            nudMaxClients.Value = Math.Max(500, Math.Min(2000, Environment.ProcessorCount * 50));
        }
        else
        {
            nudWorkers.Value = Math.Max(4, Environment.ProcessorCount * 2);
            nudMaxRequests.Value = 1000;
            nudOpcacheMemory.Value = 128;
            nudKeepAlive.Value = 15;
            nudMaxClients.Value = 256;
        }

        // Native buttons ignore BackColor under visual styles, so confirm with the caption only.
        var origText = btnApplyModeDefaults.Text;
        btnApplyModeDefaults.Text = "Applied";
        btnApplyModeDefaults.Enabled = false;
        await Task.Delay(1200);
        if (!IsDisposed)
        {
            btnApplyModeDefaults.Text = origText;
            btnApplyModeDefaults.Enabled = true;
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (!ApacheConfigWriter.IsValidServerName(txtServerName.Text.Trim()))
        {
            MessageBox.Show("Server Name must be a valid DNS host name without spaces.", "Configuration Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tabApache;
            txtServerName.Focus();
            return;
        }

        if (chkSsl.Checked)
        {
            if (string.IsNullOrWhiteSpace(txtSslCertFile.Text) ||
                string.IsNullOrWhiteSpace(txtSslKeyFile.Text))
            {
                MessageBox.Show("SSL requires a certificate file and a key file.",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabApache;
                txtSslCertFile.Focus();
                return;
            }
            if (!File.Exists(txtSslCertFile.Text.Trim()))
            {
                MessageBox.Show($"SSL certificate file not found:\n{txtSslCertFile.Text.Trim()}",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabApache;
                txtSslCertFile.Focus();
                return;
            }
            if (!File.Exists(txtSslKeyFile.Text.Trim()))
            {
                MessageBox.Show($"SSL key file not found:\n{txtSslKeyFile.Text.Trim()}",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabApache;
                txtSslKeyFile.Focus();
                return;
            }
        }

        SaveSettings();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
