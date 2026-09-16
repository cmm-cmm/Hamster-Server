using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HamsterServer.Core;
using HamsterServer.Forms;
using HamsterServer.Models;

namespace HamsterServer;

public partial class HamsterServerForm : Form
{
    private const int MaxLogLines = 2000;

    private readonly string _rootDir;
    private readonly ConfigManager _configMgr;
    private readonly PhpVersionManager _phpMgr;
    private ServerManager _server = null!;
    private LogWriter _logWriter = null!;
    private NotifyIcon _tray = null!;
    private bool _isExiting;
    private bool _updatingUI;
    private int _logLineCount;
    private volatile bool _refreshPending;
    private System.Windows.Forms.Timer _healthTimer = null!;
    private System.Windows.Forms.Timer _logTimer = null!;
    private readonly Queue<string> _logQueue = new();
    private Icon? _baseIcon;
    private Icon? _statusIcon;
    private Color _lastStatusDotColor = Color.Empty;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_SETREDRAW = 0x000B;

    // ListView has no DoubleBuffered we can set; the control's own extended style is the
    // supported way to stop an owner-drawn list from flickering while it scrolls or resizes.
    private const uint LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;
    private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public HamsterServerForm()
    {
        InitializeComponent();

        rtbLog.Font = UiTheme.CreateMonospaceFont();
        cmbLogFilter.SelectedIndex = 0;   // default: "All"

        EnableDoubleBuffering(lvServices, rtbLog);
        InitServicesList();
        InitToolbarIcons();
        InitLogTimer();

        _rootDir = ResolveRootDir();
        _logWriter = new LogWriter(_rootDir);
        _configMgr = new ConfigManager(_rootDir);
        _configMgr.Load();
        if (_configMgr.LastLoadCorrupted)
        {
            var detail = string.IsNullOrWhiteSpace(_configMgr.LastLoadError)
                ? "" : $"\n\nError: {_configMgr.LastLoadError}";
            MessageBox.Show(
                $"The configuration file is corrupted or unreadable.\nDefault settings have been loaded.{detail}",
                "Hamster Server — Config Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (_configMgr.LastLoadRecovered)
        {
            MessageBox.Show(
                "The primary configuration was corrupted or unreadable. Hamster Server recovered the last valid backup.",
                "Hamster Server — Config Recovered", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _phpMgr = new PhpVersionManager(_rootDir);
        _phpMgr.Scan();

        var configuredPhp = _configMgr.Config.Php.CurrentVersion;
        if (_phpMgr.GetVersion(configuredPhp) is null)
        {
            var latest = _phpMgr.GetLatestForBranch(configuredPhp) ?? _phpMgr.GetLatest();
            if (latest != null)
                _configMgr.Config.Php.CurrentVersion = latest.Version;
        }

        _server = new ServerManager(_rootDir, _configMgr.Config);
        _server.LogReceived += OnLog;
        _server.StatusChanged += OnStatusChanged;

        InitTray();
        InitTaskbar();
        InitHealthTimer();
        PopulatePhpVersions();
        RefreshUI();

        LogDirect($"[{DateTime.Now:HH:mm:ss}] Root: {_rootDir}");

        if (_configMgr.Config.AutoStart)
            _ = _server.StartAllAsync();
    }

    // ─── Root directory ───────────────────────────────────────────────────────

    private static string ResolveRootDir()
    {
        if (IsValidRoot(AppContext.BaseDirectory)) return AppContext.BaseDirectory;
        var dir = new DirectoryInfo(AppContext.BaseDirectory).Parent;
        for (int i = 0; i < 5 && dir != null; i++, dir = dir.Parent)
            if (IsValidRoot(dir.FullName)) return dir.FullName;
        return AppContext.BaseDirectory;
    }

    private static bool IsValidRoot(string path) =>
        Directory.Exists(Path.Combine(path, "php")) &&
        (Directory.Exists(Path.Combine(path, "apache")) ||
         Directory.Exists(Path.Combine(path, "Apache")) ||
         Directory.Exists(Path.Combine(path, "Apache24")));

    // ─── Double buffering ─────────────────────────────────────────────────────

    private static void EnableDoubleBuffering(params Control[] controls)
    {
        var prop = typeof(Control).GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var c in controls) prop?.SetValue(c, true);
    }

    // ─── Services list ────────────────────────────────────────────────────────

    private const int RowApache = 0;
    private const int RowPhp = 1;

    // Indices into _statusIcons, in the order they are added below.
    private const int IconStopped = 0;
    private const int IconRunning = 1;
    private const int IconTransition = 2;
    private const int IconError = 3;

    private ImageList _statusIcons = null!;

    private void InitServicesList()
    {
        _statusIcons = new ImageList(components) { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        foreach (var color in new[]
                 { UiTheme.StatusStopped, UiTheme.StatusRunning, UiTheme.StatusTransition, UiTheme.StatusError })
        {
            // Do NOT dispose these: ImageList only holds a reference until its handle is
            // created (when the ListView is first shown), and copies from the original then.
            // The ImageList belongs to `components`, so it and the images go with the form.
            _statusIcons.Images.Add(UiTheme.CreateStatusDot(color));
        }
        lvServices.SmallImageList = _statusIcons;

        lvServices.Items.Clear();
        lvServices.Items.Add(MakeServiceRow("Apache"));
        lvServices.Items.Add(MakeServiceRow("PHP-CGI"));
        lvServices.Items[RowApache].Selected = true;
    }

    private static ListViewItem MakeServiceRow(string module)
    {
        var item = new ListViewItem(module, IconStopped) { UseItemStyleForSubItems = false };
        item.SubItems.Add("");   // Status
        item.SubItems.Add("");   // PID
        item.SubItems.Add("");   // Port(s)
        item.SubItems.Add("");   // Version
        return item;
    }

    private void RefreshServicesList()
    {
        var apache = lvServices.Items[RowApache];
        SetServiceStatusCell(apache, _server.ApacheStatus);
        apache.SubItems[2].Text = _server.ApachePid?.ToString() ?? "—";
        apache.SubItems[3].Text = BuildPortsLabel();
        apache.SubItems[4].Text = _server.GetApacheVersion();

        var php = lvServices.Items[RowPhp];
        SetServiceStatusCell(php, _server.PhpStatus);
        php.SubItems[2].Text = _server.PhpPid?.ToString() ?? "—";
        php.SubItems[3].Text = $"{_configMgr.Config.Php.FcgiPort} (FastCGI)";
        var phpVer = _configMgr.Config.Php.CurrentVersion;
        php.SubItems[4].Text = string.IsNullOrWhiteSpace(phpVer) ? "—" : phpVer;
    }

    private static void SetServiceStatusCell(ListViewItem item, ServerStatus status)
    {
        var (text, color, icon) = status switch
        {
            ServerStatus.Running => ("Running", UiTheme.StatusRunning, IconRunning),
            ServerStatus.Starting => ("Starting…", UiTheme.StatusTransition, IconTransition),
            ServerStatus.Stopping => ("Stopping…", UiTheme.StatusTransition, IconTransition),
            ServerStatus.Error => ("Error", UiTheme.StatusError, IconError),
            _ => ("Stopped", UiTheme.StatusStopped, IconStopped)
        };
        item.SubItems[1].Text = text;
        item.SubItems[1].ForeColor = color;
        item.ImageIndex = icon;
    }

    // ─── Toolbar / status icons ───────────────────────────────────────────────

    /// <summary>
    /// Tints the toolbar and menu glyphs by meaning — green to start, red to stop, amber to
    /// restart, neutral blue for the rest — so the destructive action is never a click away
    /// from the safe one by accident.
    /// </summary>
    private void InitToolbarIcons()
    {
        SetIcon(tsbStartAll, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(tsbStopAll, UiTheme.Glyph.Stop, UiTheme.StatusError);
        SetIcon(tsbRestart, UiTheme.Glyph.Refresh, UiTheme.StatusTransition);
        SetIcon(tsbSettings, UiTheme.Glyph.Settings, UiTheme.AccentNeutral);
        SetIcon(tsbVHosts, UiTheme.Glyph.Link, UiTheme.AccentAction);
        SetIcon(tsbPorts, UiTheme.Glyph.Ports, UiTheme.AccentAction);
        SetIcon(tsbOpenWww, UiTheme.Glyph.Folder, UiTheme.StatusTransition);
        SetIcon(tsbBrowser, UiTheme.Glyph.World, UiTheme.AccentHttp);

        SetIcon(btnClearLog, UiTheme.Glyph.Clear, UiTheme.AccentNeutral);
        SetIcon(btnOpenLog, UiTheme.Glyph.Folder, UiTheme.StatusTransition);

        // File
        SetIcon(mnuFileOpenWww, UiTheme.Glyph.Folder, UiTheme.StatusTransition);
        SetIcon(mnuFileOpenLogs, UiTheme.Glyph.Folder, UiTheme.StatusTransition);
        SetIcon(mnuFileHide, UiTheme.Glyph.Minimize, UiTheme.AccentNeutral);
        SetIcon(mnuFileExit, UiTheme.Glyph.Exit, UiTheme.StatusError);

        // Service — the two per-service submenus reuse the log's Apache/PHP colours, which is
        // already how the UI tells those two apart everywhere else.
        SetIcon(mnuSvcStartAll, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(mnuSvcStopAll, UiTheme.Glyph.Stop, UiTheme.StatusError);
        SetIcon(mnuSvcRestartAll, UiTheme.Glyph.Refresh, UiTheme.StatusTransition);
        SetIcon(mnuSvcApache, UiTheme.Glyph.Server, UiTheme.LogApache);
        SetIcon(mnuSvcApacheStart, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(mnuSvcApacheStop, UiTheme.Glyph.Stop, UiTheme.StatusError);
        SetIcon(mnuSvcPhp, UiTheme.Glyph.Code, UiTheme.LogPhp);
        SetIcon(mnuSvcPhpStart, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(mnuSvcPhpStop, UiTheme.Glyph.Stop, UiTheme.StatusError);

        // Tools
        SetIcon(mnuToolsSettings, UiTheme.Glyph.Settings, UiTheme.AccentNeutral);
        SetIcon(mnuToolsPhpVersions, UiTheme.Glyph.Download, UiTheme.AccentAction);
        SetIcon(mnuToolsVHosts, UiTheme.Glyph.Link, UiTheme.AccentAction);
        SetIcon(mnuToolsPorts, UiTheme.Glyph.Ports, UiTheme.AccentAction);
        SetIcon(mnuToolsApacheConfig, UiTheme.Glyph.Page, UiTheme.LogApache);
        SetIcon(mnuToolsPhpConfig, UiTheme.Glyph.Page, UiTheme.LogPhp);
        SetIcon(mnuToolsBrowser, UiTheme.Glyph.World, UiTheme.AccentHttp);

        // Help
        SetIcon(mnuHelpAbout, UiTheme.Glyph.Info, UiTheme.AccentAction);

        // Services context menu
        SetIcon(ctxSvcStart, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(ctxSvcStop, UiTheme.Glyph.Stop, UiTheme.StatusError);
        SetIcon(ctxSvcConfig, UiTheme.Glyph.Settings, UiTheme.AccentNeutral);

        // Buttons beside the Services list — same actions as the context menu above, so the same
        // glyphs and colours.
        SetIcon(btnSvcStart, UiTheme.Glyph.Play, UiTheme.StatusRunning);
        SetIcon(btnSvcStop, UiTheme.Glyph.Stop, UiTheme.StatusError);
        SetIcon(btnSvcConfig, UiTheme.Glyph.Settings, UiTheme.AccentNeutral);
        SetIcon(btnPhpVersions, UiTheme.Glyph.Download, UiTheme.AccentAction);
    }

    private static void SetIcon(ToolStripItem item, string glyph, Color color)
    {
        item.Image = UiTheme.CreateGlyph(glyph, color);
        if (item is ToolStripButton or ToolStripMenuItem)
            item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
    }

    private static void SetIcon(Button button, string glyph, Color color) =>
        UiTheme.ApplyGlyph(button, glyph, color);

    private void UpdateGlobalStatusIcon(bool allRunning, bool anyRunning, bool anyError)
    {
        var color = UiTheme.StatusColor(allRunning, anyRunning && !allRunning, anyError);
        if (color == _lastGlobalStatusColor) return;   // avoid re-encoding on every refresh
        _lastGlobalStatusColor = color;
        lblGlobalStatus.Image?.Dispose();
        lblGlobalStatus.Image = UiTheme.CreateStatusDot(color);
    }

    private Color _lastGlobalStatusColor = Color.Empty;

    /// <summary>Index of the row the service action buttons apply to; Apache when nothing is selected.</summary>
    private int SelectedServiceRow =>
        lvServices.SelectedIndices.Count > 0 ? lvServices.SelectedIndices[0] : RowApache;

    private void lvServices_SelectedIndexChanged(object sender, EventArgs e) => UpdateServiceButtons();

    // Let the last column absorb the leftover width so the grid has no dead strip on the right.
    private void lvServices_Resize(object sender, EventArgs e)
    {
        int used = 0;
        for (int i = 0; i < lvServices.Columns.Count - 1; i++)
            used += lvServices.Columns[i].Width;

        int remaining = lvServices.ClientSize.Width - used;
        colSvcVersion.Width = Math.Max(140, remaining);
    }

    // ─── Services list painting ───────────────────────────────────────────────
    // The list is owner-drawn for exactly one reason: Windows paints the selected row with the
    // system selection colors and drops the per-cell status color, and the Apache row is
    // selected from startup — so the row the user acts on was the one row whose colour never
    // said anything. UiTheme.DrawStatusListSubItem reproduces the rest of the system rendering.

    private void lvServices_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) =>
        e.DrawDefault = true;

    // Required by the owner-draw contract; every cell is painted in DrawSubItem instead.
    private void lvServices_DrawItem(object sender, DrawListViewItemEventArgs e) { }

    private void lvServices_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) =>
        UiTheme.DrawStatusListSubItem(lvServices, e);

    private void lvServices_DoubleClick(object sender, EventArgs e) => btnSvcConfig_Click(sender, e);

    private void ctxServices_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ctxSvcStart.Enabled = btnSvcStart.Enabled;
        ctxSvcStop.Enabled = btnSvcStop.Enabled;
    }

    private void UpdateServiceButtons()
    {
        var status = SelectedServiceRow == RowPhp ? _server.PhpStatus : _server.ApacheStatus;
        btnSvcStart.Enabled = status is ServerStatus.Stopped or ServerStatus.Error;
        btnSvcStop.Enabled = status == ServerStatus.Running;
    }

    private async void btnSvcStart_Click(object sender, EventArgs e)
    {
        if (SelectedServiceRow == RowPhp) await _server.StartPhpAsync();
        else await _server.StartApacheAsync();
    }

    private async void btnSvcStop_Click(object sender, EventArgs e)
    {
        if (SelectedServiceRow == RowPhp) await _server.StopPhpAsync();
        else await _server.StopApacheAsync();
    }

    private void btnSvcConfig_Click(object sender, EventArgs e)
    {
        if (SelectedServiceRow == RowPhp) btnPhpConfig_Click(sender, e);
        else btnApacheConfig_Click(sender, e);
    }

    // ─── Menu-only commands ───────────────────────────────────────────────────

    private void mnuFileHide_Click(object sender, EventArgs e) => Hide();

    private void mnuFileExit_Click(object sender, EventArgs e) => ExitApp();

    private void mnuHelpAbout_Click(object sender, EventArgs e)
    {
        // _baseIcon, not this.Icon: UpdateTaskbarIcon replaces the window icon with a composite
        // carrying a status dot, and that dot has no business showing up as the About logo.
        using var frm = new AboutForm(
            _baseIcon ?? Icon ?? SystemIcons.Application,
            _server.GetApacheVersion(),
            _configMgr.Config.Php.CurrentVersion,
            _phpMgr.Versions.Select(v => v.Version).ToArray(),
            _rootDir);
        frm.ShowDialog(this);
    }

    // ─── Health check ─────────────────────────────────────────────────────────

    private void InitHealthTimer()
    {
        _healthTimer = new System.Windows.Forms.Timer { Interval = 10_000 }; // Fix 2.3: 30s→10s — phát hiện crash sớm hơn
        _healthTimer.Tick += (_, _) => CheckProcessHealth();
        _healthTimer.Start();
    }

    private void CheckProcessHealth()
    {
        bool dirty = false;
        if (_server.ApacheStatus == ServerStatus.Running && !_server.IsApacheRunning)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] [WARN] Apache exited unexpectedly.");
            dirty = true;
        }
        if (_server.PhpStatus == ServerStatus.Running && !_server.IsPhpRunning)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] [WARN] PHP-CGI exited unexpectedly.");
            dirty = true;
        }
        if (dirty) RefreshUI();
    }

    // ─── System tray ──────────────────────────────────────────────────────────

    private void InitTray()
    {
        _tray = new NotifyIcon { Icon = this.Icon, Text = "Hamster Server", Visible = true };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / Hide", null, (_, _) => ToggleVisibility());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Start All", null, async (_, _) => await _server.StartAllAsync());
        menu.Items.Add("Stop All", null, async (_, _) => await _server.StopAllAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open Browser", null, (_, _) => OpenBrowser());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ToggleVisibility();
    }

    private void ToggleVisibility()
    {
        if (Visible) { Hide(); }
        else { Show(); WindowState = FormWindowState.Normal; Activate(); }
    }

    // ─── Taskbar status icon ──────────────────────────────────────────────────

    private void InitTaskbar()
    {
        _baseIcon = this.Icon;
    }

    private void UpdateTaskbarIcon()
    {
        bool allRunning = _server.IsApacheRunning && _server.IsPhpRunning;
        bool anyRunning = _server.IsApacheRunning || _server.IsPhpRunning;
        bool anyError = _server.ApacheStatus == ServerStatus.Error
                        || _server.PhpStatus == ServerStatus.Error;
        bool anyStarting = _server.ApacheStatus is ServerStatus.Starting or ServerStatus.Stopping
                        || _server.PhpStatus is ServerStatus.Starting or ServerStatus.Stopping;

        Color dot;
        if (allRunning) dot = UiTheme.TaskbarDotRunning;
        else if (anyError) dot = UiTheme.TaskbarDotError;
        else if (anyStarting || anyRunning) dot = UiTheme.TaskbarDotTransition;  // starting / partial
        else dot = UiTheme.TaskbarDotStopped;

        // Fix 1.3: skip expensive PNG re-encode when dot color unchanged (only 4 possible states)
        if (dot == _lastStatusDotColor) return;
        _lastStatusDotColor = dot;

        var prev = _statusIcon;
        _statusIcon = BuildStatusIcon(_baseIcon!, dot);
        this.Icon = _statusIcon;
        _tray.Icon = _statusIcon;
        prev?.Dispose();
    }

    // Builds a multi-size .ico (16, 32, 48 px) with a status dot in the bottom-right corner.
    // Writing a proper .ico stream lets Windows pick the correct size for each context
    // (tray = 16, taskbar button = 32/48) without blurry scaling.
    private static Icon BuildStatusIcon(Icon baseIcon, Color dotColor)
    {
        int[] sizes = [16, 32, 48];
        var pngs = new byte[sizes.Length][];

        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            using var bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Draw base icon
                using var src = new Icon(baseIcon, sz, sz);
                using var srcBmp = src.ToBitmap();
                g.DrawImage(srcBmp, 0, 0, sz, sz);

                // Dot: 38% of icon size, bottom-right corner
                int dotSz = Math.Max(5, (int)(sz * 0.38f));
                int x = sz - dotSz - 1;
                int y = sz - dotSz - 1;

                using var halo = new SolidBrush(Color.White);
                g.FillEllipse(halo, x - 2, y - 2, dotSz + 4, dotSz + 4);

                using var fill = new SolidBrush(dotColor);
                g.FillEllipse(fill, x, y, dotSz, dotSz);
            }
            using var png = new System.IO.MemoryStream();
            bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            pngs[i] = png.ToArray();
        }

        // Assemble minimal .ico file: ICONDIR + ICONDIRENTRY[] + PNG data
        using var ico = new System.IO.MemoryStream();
        var w = new System.IO.BinaryWriter(ico);

        w.Write((short)0);            // reserved
        w.Write((short)1);            // type = 1 (icon)
        w.Write((short)sizes.Length); // image count

        int dataOffset = 6 + sizes.Length * 16;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            w.Write((byte)(sz >= 256 ? 0 : sz)); // width  (0 → 256)
            w.Write((byte)(sz >= 256 ? 0 : sz)); // height
            w.Write((byte)0);     // palette entries (0 = true color)
            w.Write((byte)0);     // reserved
            w.Write((short)1);    // color planes
            w.Write((short)32);   // bits per pixel
            w.Write(pngs[i].Length);
            w.Write(dataOffset);
            dataOffset += pngs[i].Length;
        }
        foreach (var png in pngs) w.Write(png);

        ico.Position = 0;
        return new Icon(ico);
    }

    // ─── PHP combo ────────────────────────────────────────────────────────────

    private void PopulatePhpVersions()
    {
        _updatingUI = true;
        cmbPhpVersion.Items.Clear();
        foreach (var v in _phpMgr.Versions)
            cmbPhpVersion.Items.Add(v.Version);

        var current = _configMgr.Config.Php.CurrentVersion;
        if (!string.IsNullOrWhiteSpace(current))
            cmbPhpVersion.SelectedItem = current;
        else if (cmbPhpVersion.Items.Count > 0)
            cmbPhpVersion.SelectedIndex = 0;
        _updatingUI = false;
    }

    // ─── UI refresh ───────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        RefreshServicesList();
        UpdateServiceButtons();

        bool allRunning = _server.IsApacheRunning && _server.IsPhpRunning;
        bool anyRunning = _server.IsApacheRunning || _server.IsPhpRunning;
        bool anyTransitioning = _server.ApacheStatus is ServerStatus.Starting or ServerStatus.Stopping ||
                                _server.PhpStatus is ServerStatus.Starting or ServerStatus.Stopping;

        bool anyError = _server.ApacheStatus == ServerStatus.Error || _server.PhpStatus == ServerStatus.Error;
        lblGlobalStatus.Text = anyError ? "Error"
                             : allRunning ? "Running"
                             : anyRunning ? "Partially running"
                             : "Stopped";
        lblGlobalStatus.ForeColor = UiTheme.StatusColor(allRunning, anyRunning && !allRunning, anyError);
        UpdateGlobalStatusIcon(allRunning, anyRunning, anyError);

        bool canStartAll = !anyTransitioning && !allRunning;
        bool canStopAll = !anyTransitioning && anyRunning;
        tsbStartAll.Enabled = mnuSvcStartAll.Enabled = canStartAll;
        tsbStopAll.Enabled = mnuSvcStopAll.Enabled = canStopAll;
        tsbRestart.Enabled = mnuSvcRestartAll.Enabled = canStopAll;

        mnuSvcApacheStart.Enabled = _server.ApacheStatus is ServerStatus.Stopped or ServerStatus.Error;
        mnuSvcApacheStop.Enabled = _server.ApacheStatus == ServerStatus.Running;
        mnuSvcPhpStart.Enabled = _server.PhpStatus is ServerStatus.Stopped or ServerStatus.Error;
        mnuSvcPhpStop.Enabled = _server.PhpStatus == ServerStatus.Running;

        _tray.Text = $"Hamster Server — {lblGlobalStatus.Text}";

        PopulateStatusBar();
        UpdateTaskbarIcon();
    }

    // Bare list — the Services grid already labels the column.
    private string BuildPortsLabel()
    {
        var cfg = _configMgr.Config.Apache;
        var extras = (cfg.ExtraPorts ?? []).Where(p => p.Enabled).Select(p => p.Port);
        return string.Join(", ", new[] { cfg.Port }.Concat(extras));
    }

    private void PopulateStatusBar()
    {
        var mode = _configMgr.Config.ServerMode == ServerMode.Production ? "Production" : "Development";
        lblStatusMode.Text = $"Mode: {mode}";
        var phpVer = _configMgr.Config.Php.CurrentVersion;
        lblStatusPhpVer.Text = string.IsNullOrWhiteSpace(phpVer) ? "PHP: —" : $"PHP: {phpVer}";
        lblStatusApacheVer.Text = $"Apache: {_server.GetApacheVersion()}";
    }

    // ─── Log pipeline (queue → 50 ms batch flush) ────────────────────────────

    private void InitLogTimer()
    {
        _logTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _logTimer.Tick += (_, _) => FlushLogQueue();
        _logTimer.Start();
    }

    // Called from any thread — just enqueue, never touch the RTB here
    private void OnLog(object? sender, string msg)
    {
        // File persistence is independent from the bounded UI queue. A frozen UI may omit old
        // display entries, but the dedicated writer still receives every log message.
        _logWriter.Enqueue(msg);
        lock (_logQueue)
        {
            if (_logQueue.Count >= 5000) _logQueue.Dequeue();  // Fix 1.3: tránh OOM khi UI freeze
            _logQueue.Enqueue(msg);
        }
    }

    private string _logFilter = "All";

    private void cmbLogFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
        _logFilter = cmbLogFilter.SelectedItem?.ToString() ?? "All";
    }

    private static bool PassesLogFilter(string msg, string filter) => filter switch
    {
        "Apache" => msg.Contains("[Apache]", StringComparison.OrdinalIgnoreCase),
        "PHP" => msg.Contains("[PHP]", StringComparison.OrdinalIgnoreCase),
        "Error" => msg.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase),
        _ => true
    };

    // Called only on UI thread by the timer — batch-flush the queue in one paint
    private void FlushLogQueue()
    {
        if (_isExiting || !rtbLog.IsHandleCreated || rtbLog.IsDisposed) return;

        string[] batch;
        lock (_logQueue)
        {
            if (_logQueue.Count == 0) return;
            batch = [.. _logQueue];
            _logQueue.Clear();
        }

        var filter = _logFilter;
        var displayText = new StringBuilder(batch.Sum(text => text.Length + Environment.NewLine.Length));
        var colorSpans = new List<LogTextSpan>();
        int appendedLines = 0;
        foreach (var text in batch)
        {
            if (!PassesLogFilter(text, filter)) continue;
            string displayEntry = LogEntryFormatter.NormalizeForDisplay(text);
            int entryStart = displayText.Length;
            displayText.Append(displayEntry).Append('\n');
            appendedLines += 1 + displayEntry.Count(ch => ch == '\n');

            foreach (var span in LogEntryFormatter.ParseSpans(displayEntry))
            {
                if (span.Kind != LogSegmentKind.Normal)
                    colorSpans.Add(span with { Start = entryStart + span.Start });
            }
        }

        if (displayText.Length == 0) return;

        // Append the visible batch once, then color only the semantic ranges.
        SendMessage(rtbLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            int appendStart = rtbLog.TextLength;
            rtbLog.SelectionStart = appendStart;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = UiTheme.LogNormal;
            rtbLog.AppendText(displayText.ToString());

            foreach (var span in colorSpans)
            {
                rtbLog.Select(appendStart + span.Start, span.Length);
                rtbLog.SelectionColor = GetLogColor(span.Kind);
            }

            _logLineCount += appendedLines;
            if (_logLineCount > MaxLogLines)
            {
                int linesToRemove = _logLineCount - MaxLogLines / 2;
                int cut = rtbLog.GetFirstCharIndexFromLine(linesToRemove);
                if (cut > 0)
                {
                    rtbLog.Select(0, cut);
                    rtbLog.SelectedText = "";
                    _logLineCount -= linesToRemove;
                }
            }

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = UiTheme.LogNormal;
            rtbLog.ScrollToCaret();
        }
        finally
        {
            SendMessage(rtbLog.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            rtbLog.Invalidate();
        }

    }

    private static Color GetLogColor(LogSegmentKind kind) => kind switch
    {
        LogSegmentKind.Timestamp => UiTheme.LogTimestamp,
        LogSegmentKind.Information => UiTheme.LogInfo,
        LogSegmentKind.Success => UiTheme.LogOk,
        LogSegmentKind.Warning => UiTheme.LogWarn,
        LogSegmentKind.Error => UiTheme.LogError,
        LogSegmentKind.Apache => UiTheme.LogApache,
        LogSegmentKind.Php => UiTheme.LogPhp,
        LogSegmentKind.Link => UiTheme.LogLink,
        _ => UiTheme.LogNormal
    };

    // Direct write for messages generated on the UI thread before the timer starts
    private void LogDirect(string msg)
    {
        _logWriter.Enqueue(msg);
        lock (_logQueue)
        {
            if (_logQueue.Count >= 5000) _logQueue.Dequeue();
            _logQueue.Enqueue(msg);
        }
    }

    // Debounce multiple rapid StatusChanged events into a single UI refresh
    private void OnStatusChanged(object? sender, EventArgs e)
    {
        // Killing the processes on exit makes their Exited handlers fire on the thread pool,
        // which raises StatusChanged after the form is gone. Marshalling to a destroyed handle
        // throws on that background thread, where nothing catches it.
        if (_isExiting || IsDisposed || !IsHandleCreated) return;
        if (_refreshPending) return;
        _refreshPending = true;
        try
        {
            if (InvokeRequired) BeginInvoke(DoRefreshUI);
            else DoRefreshUI();
        }
        catch (ObjectDisposedException) { _refreshPending = false; }
        catch (InvalidOperationException) { _refreshPending = false; }
    }

    private void DoRefreshUI()
    {
        _refreshPending = false;
        RefreshUI();
    }

    // ─── Button handlers ──────────────────────────────────────────────────────

    private async void btnStartAll_Click(object sender, EventArgs e) => await _server.StartAllAsync();
    private async void btnStopAll_Click(object sender, EventArgs e) => await _server.StopAllAsync();
    private async void btnRestart_Click(object sender, EventArgs e) => await _server.RestartAllAsync();
    private async void btnApacheStart_Click(object sender, EventArgs e) => await _server.StartApacheAsync();
    private async void btnApacheStop_Click(object sender, EventArgs e) => await _server.StopApacheAsync();
    private async void btnPhpStart_Click(object sender, EventArgs e) => await _server.StartPhpAsync();
    private async void btnPhpStop_Click(object sender, EventArgs e) => await _server.StopPhpAsync();

    private async void cmbPhpVersion_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_updatingUI) return;
        var selected = cmbPhpVersion.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(selected) || selected == _configMgr.Config.Php.CurrentVersion) return;

        // A switch stops and restarts both services; leaving the combo live let a second
        // selection interleave with the first one's restart sequence.
        cmbPhpVersion.Enabled = false;
        try
        {
            await _server.SwitchPhpVersionAsync(selected);
            await TrySaveConfigAsync();
        }
        finally
        {
            if (!IsDisposed) cmbPhpVersion.Enabled = true;
        }
    }

    private void btnPhpVersions_Click(object sender, EventArgs e)
    {
        using var form = new PhpVersionManagerForm(
            _phpMgr, _configMgr.Config.Php.CurrentVersion);
        form.ShowDialog(this);
        if (!form.VersionsChanged) return;

        PopulatePhpVersions();
        if (form.VersionInstalled)
            _server.SynchronizePhpIni();
        RefreshUI();
        LogDirect($"[{DateTime.Now:HH:mm:ss}] PHP runtime list updated.");
    }

    // Every dialog that mutates AppConfig persists it through here. Without the catch a failed
    // write (full disk, antivirus holding the file) escapes an async void handler and surfaces as
    // the generic Application.ThreadException box, which says nothing about what was lost — while
    // ExitApp has always reported it properly. The in-memory config stays applied either way, so a
    // failed save is a warning, not a reason to abandon the rest of the handler.
    private async Task TrySaveConfigAsync()
    {
        try
        {
            await _configMgr.SaveAsync();
        }
        catch (Exception ex)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] [ERROR] Could not save configuration: {ex.Message}");
            MessageBox.Show(this,
                $"Could not save configuration:\n{ex.Message}\n\n" +
                "The new settings are active for this session but will be lost on restart.",
                "Hamster Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void btnSettings_Click(object sender, EventArgs e)
    {
        _phpMgr.Scan();
        using var frm = new SettingsForm(_configMgr.Config, _rootDir);
        if (frm.ShowDialog(this) != DialogResult.OK || !frm.ConfigChanged) return;

        await TrySaveConfigAsync();
        ConfigManager.ApplyStartWithWindows(_configMgr.Config.StartWithWindows, Application.ExecutablePath);
        // The memory watchdog reads its threshold live, so it is picked up without restarting PHP.
        _server.ApplyPhpMemorySettings();
        PopulatePhpVersions();
        RefreshUI();

        if (!frm.NeedsApacheRestart && !frm.NeedsPhpRestart)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] Settings saved.");
            return;
        }

        bool apacheWasRunning = _server.IsApacheRunning;
        bool phpWasRunning = _server.IsPhpRunning;

        if (frm.NeedsPhpRestart)
            _server.SynchronizePhpIni();

        if (frm.NeedsApacheRestart && frm.NeedsPhpRestart)
        {
            if (apacheWasRunning || phpWasRunning)
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] Settings changed — restarting services…");
                await _server.StopAllAsync();
                if (apacheWasRunning) await _server.StartApacheAsync();
                if (phpWasRunning) await _server.StartPhpAsync();
            }
            else LogDirect($"[{DateTime.Now:HH:mm:ss}] Settings saved — start servers to apply.");
        }
        else if (frm.NeedsApacheRestart)
        {
            if (apacheWasRunning)
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] Apache settings changed — restarting Apache…");
                await _server.StopApacheAsync();
                await _server.StartApacheAsync();
            }
            else LogDirect($"[{DateTime.Now:HH:mm:ss}] Apache settings saved — start Apache to apply.");
        }
        else
        {
            if (phpWasRunning)
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] PHP settings changed — restarting PHP…");
                await _server.StopPhpAsync();
                await _server.StartPhpAsync();
            }
            else LogDirect($"[{DateTime.Now:HH:mm:ss}] PHP settings saved — start PHP to apply.");
        }
    }

    private async void btnVHosts_Click(object sender, EventArgs e)
    {
        using var frm = new VHostForm(_configMgr.Config, _rootDir);
        frm.ShowDialog(this);
        if (!frm.ConfigChanged) return;

        bool apacheWasRunning = _server.IsApacheRunning;
        bool phpWasRunning = _server.IsPhpRunning;

        await TrySaveConfigAsync();
        RefreshUI();

        if (frm.NeedsPhpRestart)
        {
            if (apacheWasRunning || phpWasRunning)
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] VHosts and PHP extensions updated — restarting affected services…");
                if (apacheWasRunning) await _server.StopApacheAsync();
                if (phpWasRunning) await _server.StopPhpAsync();
            }

            _server.SynchronizePhpIni();

            if (phpWasRunning) await _server.StartPhpAsync();
            if (apacheWasRunning) await _server.StartApacheAsync();

            if (!apacheWasRunning && !phpWasRunning)
                LogDirect($"[{DateTime.Now:HH:mm:ss}] VHosts and PHP extensions updated — start servers to apply.");
        }
        else if (apacheWasRunning)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] VHosts updated — restarting Apache…");
            await _server.StopApacheAsync();
            await _server.StartApacheAsync();
        }
        else
            LogDirect($"[{DateTime.Now:HH:mm:ss}] VHosts updated — start Apache to apply.");
    }

    private async void btnPorts_Click(object sender, EventArgs e)
    {
        using var frm = new PortsForm(_configMgr.Config);
        frm.ShowDialog(this);
        if (!frm.ConfigChanged) return;

        await TrySaveConfigAsync();
        RefreshUI();

        if (_server.IsApacheRunning)
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] Ports updated — restarting Apache to apply…");
            await _server.StopApacheAsync();
            await _server.StartApacheAsync();
        }
        else
        {
            LogDirect($"[{DateTime.Now:HH:mm:ss}] Ports updated — start Apache to apply.");
        }
    }

    private void btnOpenWww_Click(object sender, EventArgs e)
    {
        var www = Path.Combine(_rootDir, "www");
        Directory.CreateDirectory(www);
        Process.Start("explorer.exe", www);
    }

    private void btnBrowser_Click(object sender, EventArgs e) => OpenBrowser();

    private void btnClearLog_Click(object sender, EventArgs e)
    {
        lock (_logQueue) _logQueue.Clear();
        rtbLog.Clear();
        _logLineCount = 0;
    }

    private void btnOpenLog_Click(object sender, EventArgs e)
    {
        var logDir = Path.Combine(_rootDir, "config", "logs", "server");
        Directory.CreateDirectory(logDir);
        Process.Start("explorer.exe", logDir);
    }

    private async void btnApacheConfig_Click(object sender, EventArgs e)
    {
        using var frm = new ApacheConfigForm(_configMgr.Config, _rootDir, _server);
        if (frm.ShowDialog(this) == DialogResult.OK && frm.ConfigChanged)
        {
            await TrySaveConfigAsync();
            if (_server.IsApacheRunning)
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] Apache config saved — restarting…");
                await _server.StopApacheAsync();
                await _server.StartApacheAsync();
            }
            else
            {
                LogDirect($"[{DateTime.Now:HH:mm:ss}] Apache config saved — start Apache to apply.");
            }
        }
    }

    private void btnPhpConfig_Click(object sender, EventArgs e)
    {
        var ver = cmbPhpVersion.SelectedItem?.ToString();
        var phpInfo = string.IsNullOrWhiteSpace(ver) ? null : _phpMgr.Versions.FirstOrDefault(v => v.Version == ver);
        if (phpInfo == null)
        {
            MessageBox.Show("No PHP version selected.", "Hamster Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var frm = new PhpConfigForm(phpInfo, _server);
        frm.ShowDialog(this);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void OpenBrowser()
    {
        var url = $"http://localhost:{_configMgr.Config.Apache.Port}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void ExitApp()
    {
        if (_isExiting) return;
        _healthTimer.Stop();
        _logTimer.Stop();
        FlushLogQueue();        // drain whatever is still queued while the UI is alive
        _isExiting = true;
        _tray.Visible = false;

        // Shutdown runs synchronously on the UI thread on purpose — the process tree has to be
        // gone before Application.Exit, and ServerManager.Dispose sweeps every httpd/php-cgi on
        // the machine to catch mpm_winnt children that outlived their parent. That sweep plus the
        // log flush can take a noticeable moment, so say so instead of freezing silently.
        UseWaitCursor = true;
        Enabled = false;
        try
        {
            _configMgr.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save configuration:\n{ex.Message}", "Hamster Server",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _server.Dispose();
            _logWriter.CompleteAndFlush(TimeSpan.FromSeconds(2));
            Application.Exit();
        }
    }

    // ─── Form events ──────────────────────────────────────────────────────────

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Dock layout is only final once the form is on screen, and Resize does not
        // fire when the size never changed — size the last column explicitly here.
        lvServices_Resize(this, EventArgs.Empty);

        SendMessage(lvServices.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,
            new IntPtr(LVS_EX_DOUBLEBUFFER), new IntPtr(LVS_EX_DOUBLEBUFFER));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && _configMgr.Config.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBalloonTip(3000, "Hamster Server",
                "Hamster Server is running in the system tray. Right-click the tray icon to exit.",
                ToolTipIcon.Info);
            return;
        }
        if (!_isExiting) { e.Cancel = true; ExitApp(); return; }
        base.OnFormClosing(e);
    }
}
