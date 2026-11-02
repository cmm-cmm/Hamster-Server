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

    private readonly Action<IWin32Window>? _openPhpConfig;

    /// <param name="openPhpConfig">
    /// Opens PHP Configuration on top of this dialog: the one place php.ini directives and
    /// extensions are edited. Null hides the button (tests construct the form without a host).
    /// </param>
    public SettingsForm(AppConfig config, string rootDir, Action<IWin32Window>? openPhpConfig = null)
    {
        InitializeComponent();
        cboServerMode.DataSource = Enum.GetValues<ServerMode>();
        _config = config;
        _rootDir = rootDir;
        _openPhpConfig = openPhpConfig;
        btnOpenPhpConfig.Visible = openPhpConfig is not null;
        LoadSettings();
        chkSsl.CheckedChanged += (_, _) => UpdateSslCertVisibility();
        chkOpcache.CheckedChanged += (_, _) => UpdateOpcacheVisibility();
        chkAllowLanAccess.CheckedChanged += (_, _) => UpdateRiskHighlights();
        nudWorkers.ValueChanged += (_, _) => UpdateRiskHighlights();
        // The automatic threshold is derived from the worker count, and the manual warning is
        // measured against it, so both follow either field changing.
        nudWorkers.ValueChanged += (_, _) => UpdateMemoryRestartHint();
        nudWorkers.ValueChanged += (_, _) => UpdateWorkersHint();
        nudMemoryRestart.ValueChanged += (_, _) => UpdateMemoryRestartHint();
        chkMemoryRestartAuto.CheckedChanged += (_, _) => UpdateMemoryRestartHint();
        UpdateSslCertVisibility();
        UpdateOpcacheVisibility();
        UpdateRiskHighlights();
        UpdateMemoryRestartHint();
        UpdateWorkersHint();
        InitIcons();
        InitTooltips();
    }

    /// <summary>
    /// Reports what this machine can carry, so the worker count is chosen against the hardware
    /// rather than guessed. CPU caps the useful number; RAM caps the safe one, and on a small
    /// machine RAM is the binding limit — which is worth saying out loud rather than only
    /// colouring the field red after the fact.
    /// </summary>
    private void UpdateWorkersHint()
    {
        // memory_limit is edited in PHP Configuration; read it live so the hint follows a change
        // made there while this dialog is open.
        var advice = ServerManager.SuggestWorkers(_config.Php.MemoryLimit);
        int chosen = (int)nudWorkers.Value;

        string machine = advice.TotalRamMb > 0
            ? $"{advice.Cores} cores, {advice.TotalRamMb / 1024.0:0.#} GB RAM"
            : $"{advice.Cores} cores";
        string why = advice.RamLimited ? ", RAM-limited" : "";

        lblWorkersHint.Text = $"Suggested: {advice.Workers}  ({machine}{why}, {_config.Php.MemoryLimit} MB memory_limit)";
        lblWorkersHint.ForeColor = chosen > advice.Workers
            ? UiTheme.StatusTransition
            : SystemColors.GrayText;
    }

    /// <summary>
    /// Shows what the pool restart threshold will actually be, because the number on its own says
    /// nothing useful: whether it is generous or punishing depends entirely on the worker count.
    /// In Auto the box is disabled and shows the derived value; typed by hand, the field is
    /// highlighted when it sits so close to the pool's floor that recycling would be constant.
    /// </summary>
    private void UpdateMemoryRestartHint()
    {
        int workers = (int)nudWorkers.Value;
        int floor = workers * ServerManager.WorkerBaselineMb;
        int auto = ServerManager.AutoMemoryThresholdMb(workers);
        bool isAuto = chkMemoryRestartAuto.Checked;

        nudMemoryRestart.Enabled = !isAuto;
        if (isAuto)
        {
            nudMemoryRestart.Value = Math.Clamp(auto, nudMemoryRestart.Minimum, nudMemoryRestart.Maximum);
            lblMemoryRestartHint.Text =
                $"= {auto} MB  ({workers} × {ServerManager.WorkerBaselineMb} MB at rest × {ServerManager.AutoThresholdFactor})";
            lblMemoryRestartHint.ForeColor = SystemColors.GrayText;
            nudMemoryRestart.BackColor = UiTheme.FieldNormal;
            return;
        }

        int chosen = (int)nudMemoryRestart.Value;
        if (chosen == 0)
        {
            lblMemoryRestartHint.Text = "0 = never restart; reclaim by hand.";
            lblMemoryRestartHint.ForeColor = SystemColors.GrayText;
            nudMemoryRestart.BackColor = UiTheme.FieldNormal;
            return;
        }

        // Under twice the floor the pool is recycled every time it does any real work.
        bool tooTight = chosen < floor * 2;
        lblMemoryRestartHint.Text = tooTight
            ? $"Low — {floor} MB at rest. Suggest {auto} MB."
            : $"~{floor} MB at rest. Suggested: {auto} MB.";
        lblMemoryRestartHint.ForeColor = tooTight ? UiTheme.StatusTransition : SystemColors.GrayText;
        nudMemoryRestart.BackColor = tooTight ? UiTheme.FieldWarning : UiTheme.FieldNormal;
    }

    /// <summary>
    /// Only the action buttons get a glyph. OK / Cancel / Browse… stay bare because Windows
    /// dialog buttons do not carry icons.
    /// </summary>
    private void InitIcons()
    {
        UiTheme.ApplyGlyph(btnApplyModeDefaults, UiTheme.Glyph.Wrench, UiTheme.AccentNeutral);
        // Same glyph and colour as Tools > PHP Configuration, which this button opens.
        UiTheme.ApplyGlyph(btnOpenPhpConfig, UiTheme.Glyph.Page, UiTheme.LogPhp);
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
        long projectedMb = (long)nudWorkers.Value * _config.Php.MemoryLimit;
        bool overcommitted = totalRamMb > 0 && projectedMb > totalRamMb * 0.70;

        nudWorkers.BackColor = overcommitted ? UiTheme.FieldWarning : UiTheme.FieldNormal;

        lblMemoryWarning.Visible = overcommitted;
        if (overcommitted)
            lblMemoryWarning.Text =
                $"{nudWorkers.Value} workers × {_config.Php.MemoryLimit} MB memory_limit = {projectedMb} MB worst case, " +
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
        tt.SetToolTip(nudWorkers,
            "PHP-CGI worker processes. The suggestion beside the box is the lower of what the CPU "
            + "can use (4-8, measured) and what RAM allows at the configured memory limit.");
        tt.SetToolTip(nudMaxRequests, "Requests before a worker recycles. 0 = never recycle (required for FastCGI connection reuse).");
        tt.SetToolTip(nudMemoryRestart, "Restart the whole PHP pool once it uses this much memory, and only while it is idle. 0 = never.");
        tt.SetToolTip(chkMemoryRestartAuto,
            "Derive the threshold from the worker count instead of fixing it. A number that suits "
            + "4 workers leaves 12 workers being recycled every few minutes.");
        tt.SetToolTip(chkUseBundledCaCertificate,
            $"Configure cURL and OpenSSL to use {Path.Combine(_rootDir, "ssl", "cacert.pem")} for HTTPS requests.");
        tt.SetToolTip(cboServerMode, "Development: verbose errors, no caching.  Production: optimized config.");
        tt.SetToolTip(btnApplyModeDefaults, "Pre-fill workers, memory, and timeouts with optimal values for the selected mode.");
        tt.SetToolTip(btnBrowseSslCert, "Browse for SSL certificate file (.crt / .pem)");
        tt.SetToolTip(btnBrowseSslKey, "Browse for SSL private key file (.key / .pem)");
        tt.SetToolTip(btnOpenPhpConfig,
            "Edit memory_limit, upload sizes, execution time, error reporting, timezone and extensions.");
        tt.SetToolTip(chkAllowLanAccess, "Off by default: Apache listens only on 127.0.0.1. Enable only on trusted networks.");
    }

    // ─── php.ini (edited in PHP Configuration) ────────────────────────────────

    private void btnOpenPhpConfig_Click(object sender, EventArgs e)
    {
        if (_openPhpConfig is null) return;
        _openPhpConfig(this);
        // memory_limit may have changed there; both hints are measured against it.
        UpdateWorkersHint();
        UpdateRiskHighlights();
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
        nudMemoryRestart.Value = _config.Php.MemoryRestartThreshold;
        chkMemoryRestartAuto.Checked = _config.Php.MemoryRestartAuto;
        chkOpcache.Checked = _config.Php.EnableOpcache;
        nudOpcacheMemory.Value = _config.Php.OpcacheMemory;
        chkUseBundledCaCertificate.Checked = _config.Php.UseBundledCaCertificate;

        // General
        chkMinimizeToTray.Checked = _config.MinimizeToTray;
        chkAutoStart.Checked = _config.AutoStart;
        chkStartWithWindows.Checked = _config.StartWithWindows;
    }

    private void SaveSettings()
    {
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

        // memory_limit, max_execution_time, timezone and extensions are not here: PhpConfigForm
        // owns them and applies its own changes, so this dialog neither edits nor restarts for them.
        NeedsPhpRestart =
            _config.Php.FcgiPort != (int)nudFcgiPort.Value ||
            _config.Php.Workers != (int)nudWorkers.Value ||
            _config.Php.MaxRequests != (int)nudMaxRequests.Value ||
            _config.Php.EnableOpcache != chkOpcache.Checked ||
            _config.Php.OpcacheMemory != (int)nudOpcacheMemory.Value ||
            _config.Php.UseBundledCaCertificate != chkUseBundledCaCertificate.Checked;

        bool generalChanged =
            _config.MinimizeToTray != chkMinimizeToTray.Checked ||
            _config.AutoStart != chkAutoStart.Checked ||
            _config.StartWithWindows != chkStartWithWindows.Checked;

        // Deliberately not part of NeedsPhpRestart: ServerManager re-reads the threshold through
        // ApplyPhpMemorySettings, so changing it must not bounce a healthy pool.
        bool memoryWatchdogChanged =
            _config.Php.MemoryRestartThreshold != (int)nudMemoryRestart.Value ||
            _config.Php.MemoryRestartAuto != chkMemoryRestartAuto.Checked;

        ConfigChanged = NeedsApacheRestart || NeedsPhpRestart || generalChanged || memoryWatchdogChanged;

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
        _config.Php.MemoryRestartAuto = chkMemoryRestartAuto.Checked;
        _config.Php.MemoryRestartThreshold = (int)nudMemoryRestart.Value;
        _config.Php.EnableOpcache = chkOpcache.Checked;
        _config.Php.OpcacheMemory = (int)nudOpcacheMemory.Value;
        _config.Php.UseBundledCaCertificate = chkUseBundledCaCertificate.Checked;

        _config.MinimizeToTray = chkMinimizeToTray.Checked;
        _config.AutoStart = chkAutoStart.Checked;
        _config.StartWithWindows = chkStartWithWindows.Checked;
    }

    private async void btnApplyModeDefaults_Click(object sender, EventArgs e)
    {
        bool isProd = (ServerMode)cboServerMode.SelectedItem! == ServerMode.Production;

        // Worker count comes from the same advice shown beside the field, so the button and the
        // hint can never disagree: capped by what the CPU can use, then cut to what RAM allows.
        // MaxRequests stays 0 in both modes so FastCGI connection pooling can be emitted — see
        // ApacheConfigWriter.FcgiReuse and doc/PERFORMANCE_EVALUATION.md.
        if (isProd)
        {
            nudOpcacheMemory.Value = 256;
            nudKeepAlive.Value = 5;
            nudMaxClients.Value = Math.Max(500, Math.Min(2000, Environment.ProcessorCount * 50));
        }
        else
        {
            nudOpcacheMemory.Value = 128;
            nudKeepAlive.Value = 15;
            nudMaxClients.Value = 256;
        }

        nudMaxRequests.Value = 0;
        // After the memory limit, since the advice is measured against it.
        nudWorkers.Value = ServerManager.SuggestWorkers(_config.Php.MemoryLimit).Workers;

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
