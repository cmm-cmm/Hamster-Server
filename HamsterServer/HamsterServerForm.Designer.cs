namespace HamsterServer;

partial class HamsterServerForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // A static event: without this the form would never be collected.
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            components?.Dispose();
            _tray?.Dispose();
            _server?.Dispose();
            _logWriter?.Dispose();
            _statusIcon?.Dispose();
            _taskbarIcon?.Dispose();
            _healthTimer?.Dispose();
            _logTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HamsterServerForm));
        menuStrip = new MenuStrip();
        mnuFile = new ToolStripMenuItem();
        mnuFileOpenWww = new ToolStripMenuItem();
        mnuFileOpenLogs = new ToolStripMenuItem();
        mnuFileSep1 = new ToolStripSeparator();
        mnuFileHide = new ToolStripMenuItem();
        mnuFileExit = new ToolStripMenuItem();
        mnuService = new ToolStripMenuItem();
        mnuSvcStartAll = new ToolStripMenuItem();
        mnuSvcStopAll = new ToolStripMenuItem();
        mnuSvcRestartAll = new ToolStripMenuItem();
        mnuSvcSep1 = new ToolStripSeparator();
        mnuSvcApache = new ToolStripMenuItem();
        mnuSvcApacheStart = new ToolStripMenuItem();
        mnuSvcApacheStop = new ToolStripMenuItem();
        mnuSvcPhp = new ToolStripMenuItem();
        mnuSvcPhpStart = new ToolStripMenuItem();
        mnuSvcPhpStop = new ToolStripMenuItem();
        mnuTools = new ToolStripMenuItem();
        mnuToolsSettings = new ToolStripMenuItem();
        mnuToolsPhpVersions = new ToolStripMenuItem();
        mnuToolsVHosts = new ToolStripMenuItem();
        mnuToolsPorts = new ToolStripMenuItem();
        mnuToolsSep1 = new ToolStripSeparator();
        mnuToolsApacheConfig = new ToolStripMenuItem();
        mnuToolsPhpConfig = new ToolStripMenuItem();
        mnuToolsSep2 = new ToolStripSeparator();
        mnuToolsBrowser = new ToolStripMenuItem();
        mnuHelp = new ToolStripMenuItem();
        mnuHelpAbout = new ToolStripMenuItem();
        toolStrip = new ToolStrip();
        tsbStartAll = new ToolStripButton();
        tsbStopAll = new ToolStripButton();
        tsbRestart = new ToolStripButton();
        tsbSep1 = new ToolStripSeparator();
        tsbSettings = new ToolStripButton();
        tsbVHosts = new ToolStripButton();
        tsbPorts = new ToolStripButton();
        tsbSep2 = new ToolStripSeparator();
        tsbOpenWww = new ToolStripButton();
        tsbBrowser = new ToolStripButton();
        statusStrip = new StatusStrip();
        lblGlobalStatus = new ToolStripStatusLabel();
        lblStatusSpring = new ToolStripStatusLabel();
        lblStatusMode = new ToolStripStatusLabel();
        lblStatusPhpVer = new ToolStripStatusLabel();
        lblStatusApacheVer = new ToolStripStatusLabel();
        lblStatusPhpUpdate = new ToolStripStatusLabel();
        splitMain = new SplitContainer();
        grpServices = new GroupBox();
        lvServices = new ListView();
        colSvcModule = new ColumnHeader();
        colSvcStatus = new ColumnHeader();
        colSvcPid = new ColumnHeader();
        colSvcPorts = new ColumnHeader();
        colSvcVersion = new ColumnHeader();
        ctxServices = new ContextMenuStrip(components);
        ctxSvcStart = new ToolStripMenuItem();
        ctxSvcStop = new ToolStripMenuItem();
        ctxSvcSep1 = new ToolStripSeparator();
        ctxSvcConfig = new ToolStripMenuItem();
        panSvcActions = new Panel();
        flpSvcRight = new FlowLayoutPanel();
        btnSvcStart = new Button();
        btnSvcStop = new Button();
        btnSvcConfig = new Button();
        flpSvcLeft = new FlowLayoutPanel();
        lblPhpVersionCap = new Label();
        cmbPhpVersion = new ComboBox();
        btnPhpVersions = new Button();
        grpLog = new GroupBox();
        rtbLog = new RichTextBox();
        tsLog = new ToolStrip();
        tslLogFilter = new ToolStripLabel();
        cmbLogFilter = new ToolStripComboBox();
        tsLogSep1 = new ToolStripSeparator();
        btnClearLog = new ToolStripButton();
        btnOpenLog = new ToolStripButton();
        menuStrip.SuspendLayout();
        toolStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        grpServices.SuspendLayout();
        ctxServices.SuspendLayout();
        panSvcActions.SuspendLayout();
        flpSvcRight.SuspendLayout();
        flpSvcLeft.SuspendLayout();
        grpLog.SuspendLayout();
        tsLog.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuService, mnuTools, mnuHelp });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(884, 24);
        menuStrip.TabIndex = 0;
        // 
        // mnuFile
        // 
        mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuFileOpenWww, mnuFileOpenLogs, mnuFileSep1, mnuFileHide, mnuFileExit });
        mnuFile.Name = "mnuFile";
        mnuFile.Size = new Size(37, 20);
        mnuFile.Text = "&File";
        // 
        // mnuFileOpenWww
        // 
        mnuFileOpenWww.Name = "mnuFileOpenWww";
        mnuFileOpenWww.ShortcutKeys = Keys.Control | Keys.W;
        mnuFileOpenWww.Size = new Size(235, 22);
        mnuFileOpenWww.Text = "Open &Document Root";
        mnuFileOpenWww.Click += btnOpenWww_Click;
        // 
        // mnuFileOpenLogs
        // 
        mnuFileOpenLogs.Name = "mnuFileOpenLogs";
        mnuFileOpenLogs.ShortcutKeys = Keys.Control | Keys.L;
        mnuFileOpenLogs.Size = new Size(235, 22);
        mnuFileOpenLogs.Text = "Open &Log Folder";
        mnuFileOpenLogs.Click += btnOpenLog_Click;
        // 
        // mnuFileSep1
        // 
        mnuFileSep1.Name = "mnuFileSep1";
        mnuFileSep1.Size = new Size(232, 6);
        // 
        // mnuFileHide
        // 
        mnuFileHide.Name = "mnuFileHide";
        mnuFileHide.Size = new Size(235, 22);
        mnuFileHide.Text = "&Hide to Tray";
        mnuFileHide.Click += mnuFileHide_Click;
        // 
        // mnuFileExit
        // 
        mnuFileExit.Name = "mnuFileExit";
        mnuFileExit.ShortcutKeys = Keys.Alt | Keys.F4;
        mnuFileExit.ShowShortcutKeys = false;
        mnuFileExit.Size = new Size(235, 22);
        mnuFileExit.Text = "E&xit";
        mnuFileExit.Click += mnuFileExit_Click;
        // 
        // mnuService
        // 
        mnuService.DropDownItems.AddRange(new ToolStripItem[] { mnuSvcStartAll, mnuSvcStopAll, mnuSvcRestartAll, mnuSvcSep1, mnuSvcApache, mnuSvcPhp });
        mnuService.Name = "mnuService";
        mnuService.Size = new Size(56, 20);
        mnuService.Text = "&Service";
        // 
        // mnuSvcStartAll
        // 
        mnuSvcStartAll.Name = "mnuSvcStartAll";
        mnuSvcStartAll.ShortcutKeys = Keys.F5;
        mnuSvcStartAll.Size = new Size(146, 22);
        mnuSvcStartAll.Text = "&Start All";
        mnuSvcStartAll.Click += btnStartAll_Click;
        // 
        // mnuSvcStopAll
        // 
        mnuSvcStopAll.Name = "mnuSvcStopAll";
        mnuSvcStopAll.ShortcutKeys = Keys.F6;
        mnuSvcStopAll.Size = new Size(146, 22);
        mnuSvcStopAll.Text = "S&top All";
        mnuSvcStopAll.Click += btnStopAll_Click;
        // 
        // mnuSvcRestartAll
        // 
        mnuSvcRestartAll.Name = "mnuSvcRestartAll";
        mnuSvcRestartAll.ShortcutKeys = Keys.F7;
        mnuSvcRestartAll.Size = new Size(146, 22);
        mnuSvcRestartAll.Text = "&Restart All";
        mnuSvcRestartAll.Click += btnRestart_Click;
        // 
        // mnuSvcSep1
        // 
        mnuSvcSep1.Name = "mnuSvcSep1";
        mnuSvcSep1.Size = new Size(143, 6);
        // 
        // mnuSvcApache
        // 
        mnuSvcApache.DropDownItems.AddRange(new ToolStripItem[] { mnuSvcApacheStart, mnuSvcApacheStop });
        mnuSvcApache.Name = "mnuSvcApache";
        mnuSvcApache.Size = new Size(146, 22);
        mnuSvcApache.Text = "&Apache";
        // 
        // mnuSvcApacheStart
        // 
        mnuSvcApacheStart.Name = "mnuSvcApacheStart";
        mnuSvcApacheStart.Size = new Size(98, 22);
        mnuSvcApacheStart.Text = "&Start";
        mnuSvcApacheStart.Click += btnApacheStart_Click;
        // 
        // mnuSvcApacheStop
        // 
        mnuSvcApacheStop.Name = "mnuSvcApacheStop";
        mnuSvcApacheStop.Size = new Size(98, 22);
        mnuSvcApacheStop.Text = "S&top";
        mnuSvcApacheStop.Click += btnApacheStop_Click;
        // 
        // mnuSvcPhp
        // 
        mnuSvcPhp.DropDownItems.AddRange(new ToolStripItem[] { mnuSvcPhpStart, mnuSvcPhpStop });
        mnuSvcPhp.Name = "mnuSvcPhp";
        mnuSvcPhp.Size = new Size(146, 22);
        mnuSvcPhp.Text = "&PHP-CGI";
        // 
        // mnuSvcPhpStart
        // 
        mnuSvcPhpStart.Name = "mnuSvcPhpStart";
        mnuSvcPhpStart.Size = new Size(98, 22);
        mnuSvcPhpStart.Text = "&Start";
        mnuSvcPhpStart.Click += btnPhpStart_Click;
        // 
        // mnuSvcPhpStop
        // 
        mnuSvcPhpStop.Name = "mnuSvcPhpStop";
        mnuSvcPhpStop.Size = new Size(98, 22);
        mnuSvcPhpStop.Text = "S&top";
        mnuSvcPhpStop.Click += btnPhpStop_Click;
        // 
        // mnuTools
        // 
        mnuTools.DropDownItems.AddRange(new ToolStripItem[] { mnuToolsSettings, mnuToolsPhpVersions, mnuToolsVHosts, mnuToolsPorts, mnuToolsSep1, mnuToolsApacheConfig, mnuToolsPhpConfig, mnuToolsSep2, mnuToolsBrowser });
        mnuTools.Name = "mnuTools";
        mnuTools.Size = new Size(47, 20);
        mnuTools.Text = "&Tools";
        // 
        // mnuToolsSettings
        // 
        mnuToolsSettings.Name = "mnuToolsSettings";
        mnuToolsSettings.Size = new Size(200, 22);
        mnuToolsSettings.Text = "&Settings…";
        mnuToolsSettings.Click += btnSettings_Click;
        // 
        // mnuToolsPhpVersions
        // 
        mnuToolsPhpVersions.Name = "mnuToolsPhpVersions";
        mnuToolsPhpVersions.Size = new Size(200, 22);
        mnuToolsPhpVersions.Text = "PHP &Version Manager…";
        mnuToolsPhpVersions.Click += btnPhpVersions_Click;
        // 
        // mnuToolsVHosts
        // 
        mnuToolsVHosts.Name = "mnuToolsVHosts";
        mnuToolsVHosts.Size = new Size(200, 22);
        mnuToolsVHosts.Text = "Vir&tual Hosts…";
        mnuToolsVHosts.Click += btnVHosts_Click;
        // 
        // mnuToolsPorts
        // 
        mnuToolsPorts.Name = "mnuToolsPorts";
        mnuToolsPorts.Size = new Size(200, 22);
        mnuToolsPorts.Text = "&Ports…";
        mnuToolsPorts.Click += btnPorts_Click;
        // 
        // mnuToolsSep1
        // 
        mnuToolsSep1.Name = "mnuToolsSep1";
        mnuToolsSep1.Size = new Size(197, 6);
        // 
        // mnuToolsApacheConfig
        // 
        mnuToolsApacheConfig.Name = "mnuToolsApacheConfig";
        mnuToolsApacheConfig.Size = new Size(200, 22);
        mnuToolsApacheConfig.Text = "&Apache Configuration…";
        mnuToolsApacheConfig.Click += btnApacheConfig_Click;
        // 
        // mnuToolsPhpConfig
        // 
        mnuToolsPhpConfig.Name = "mnuToolsPhpConfig";
        mnuToolsPhpConfig.Size = new Size(200, 22);
        mnuToolsPhpConfig.Text = "P&HP Configuration…";
        mnuToolsPhpConfig.Click += btnPhpConfig_Click;
        // 
        // mnuToolsSep2
        // 
        mnuToolsSep2.Name = "mnuToolsSep2";
        mnuToolsSep2.Size = new Size(197, 6);
        // 
        // mnuToolsBrowser
        // 
        mnuToolsBrowser.Name = "mnuToolsBrowser";
        mnuToolsBrowser.Size = new Size(200, 22);
        mnuToolsBrowser.Text = "Open in &Browser";
        mnuToolsBrowser.Click += btnBrowser_Click;
        // 
        // mnuHelp
        // 
        mnuHelp.DropDownItems.AddRange(new ToolStripItem[] { mnuHelpAbout });
        mnuHelp.Name = "mnuHelp";
        mnuHelp.Size = new Size(44, 20);
        mnuHelp.Text = "&Help";
        // 
        // mnuHelpAbout
        // 
        mnuHelpAbout.Name = "mnuHelpAbout";
        mnuHelpAbout.Size = new Size(190, 22);
        mnuHelpAbout.Text = "&About Hamster Server";
        mnuHelpAbout.Click += mnuHelpAbout_Click;
        // 
        // toolStrip
        // 
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { tsbStartAll, tsbStopAll, tsbRestart, tsbSep1, tsbSettings, tsbVHosts, tsbPorts, tsbSep2, tsbOpenWww, tsbBrowser });
        toolStrip.Location = new Point(0, 24);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(884, 25);
        toolStrip.TabIndex = 1;
        // 
        // tsbStartAll
        // 
        tsbStartAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbStartAll.Name = "tsbStartAll";
        tsbStartAll.Size = new Size(52, 22);
        tsbStartAll.Text = "Start All";
        tsbStartAll.ToolTipText = "Start Apache and PHP together (F5)";
        tsbStartAll.Click += btnStartAll_Click;
        // 
        // tsbStopAll
        // 
        tsbStopAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbStopAll.Name = "tsbStopAll";
        tsbStopAll.Size = new Size(52, 22);
        tsbStopAll.Text = "Stop All";
        tsbStopAll.ToolTipText = "Stop all running services (F6)";
        tsbStopAll.Click += btnStopAll_Click;
        // 
        // tsbRestart
        // 
        tsbRestart.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbRestart.Name = "tsbRestart";
        tsbRestart.Size = new Size(47, 22);
        tsbRestart.Text = "Restart";
        tsbRestart.ToolTipText = "Stop then restart all services (F7)";
        tsbRestart.Click += btnRestart_Click;
        // 
        // tsbSep1
        // 
        tsbSep1.Name = "tsbSep1";
        tsbSep1.Size = new Size(6, 25);
        // 
        // tsbSettings
        // 
        tsbSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbSettings.Name = "tsbSettings";
        tsbSettings.Size = new Size(53, 22);
        tsbSettings.Text = "Settings";
        tsbSettings.ToolTipText = "Open settings: Apache, PHP, general options";
        tsbSettings.Click += btnSettings_Click;
        // 
        // tsbVHosts
        // 
        tsbVHosts.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbVHosts.Name = "tsbVHosts";
        tsbVHosts.Size = new Size(78, 22);
        tsbVHosts.Text = "Virtual Hosts";
        tsbVHosts.ToolTipText = "Manage Apache virtual host definitions";
        tsbVHosts.Click += btnVHosts_Click;
        // 
        // tsbPorts
        // 
        tsbPorts.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbPorts.Name = "tsbPorts";
        tsbPorts.Size = new Size(38, 22);
        tsbPorts.Text = "Ports";
        tsbPorts.ToolTipText = "Configure Apache listening ports";
        tsbPorts.Click += btnPorts_Click;
        // 
        // tsbSep2
        // 
        tsbSep2.Name = "tsbSep2";
        tsbSep2.Size = new Size(6, 25);
        // 
        // tsbOpenWww
        // 
        tsbOpenWww.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbOpenWww.Name = "tsbOpenWww";
        tsbOpenWww.Size = new Size(95, 22);
        tsbOpenWww.Text = "Document Root";
        tsbOpenWww.ToolTipText = "Open the www document root in Explorer";
        tsbOpenWww.Click += btnOpenWww_Click;
        // 
        // tsbBrowser
        // 
        tsbBrowser.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tsbBrowser.Name = "tsbBrowser";
        tsbBrowser.Size = new Size(53, 22);
        tsbBrowser.Text = "Browser";
        tsbBrowser.ToolTipText = "Open localhost in the default web browser";
        tsbBrowser.Click += btnBrowser_Click;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { lblGlobalStatus, lblStatusSpring, lblStatusPhpUpdate, lblStatusMode, lblStatusPhpVer, lblStatusApacheVer });
        statusStrip.Location = new Point(0, 537);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(884, 24);
        statusStrip.TabIndex = 3;
        // 
        // lblGlobalStatus
        // 
        lblGlobalStatus.Name = "lblGlobalStatus";
        lblGlobalStatus.Size = new Size(51, 19);
        lblGlobalStatus.Text = "Stopped";
        // 
        // lblStatusSpring
        // 
        lblStatusSpring.Name = "lblStatusSpring";
        lblStatusSpring.Size = new Size(578, 19);
        lblStatusSpring.Spring = true;
        // 
        // lblStatusMode
        // 
        lblStatusMode.BorderSides = ToolStripStatusLabelBorderSides.Left;
        lblStatusMode.BorderStyle = Border3DStyle.Etched;
        lblStatusMode.Name = "lblStatusMode";
        lblStatusMode.Size = new Size(119, 19);
        lblStatusMode.Text = "Mode: Development";
        // 
        // lblStatusPhpVer
        // 
        lblStatusPhpVer.BorderSides = ToolStripStatusLabelBorderSides.Left;
        lblStatusPhpVer.BorderStyle = Border3DStyle.Etched;
        lblStatusPhpVer.Name = "lblStatusPhpVer";
        lblStatusPhpVer.Size = new Size(52, 19);
        lblStatusPhpVer.Text = "PHP: —";
        // 
        // lblStatusApacheVer
        // 
        lblStatusApacheVer.BorderSides = ToolStripStatusLabelBorderSides.Left;
        lblStatusApacheVer.BorderStyle = Border3DStyle.Etched;
        lblStatusApacheVer.Name = "lblStatusApacheVer";
        lblStatusApacheVer.Size = new Size(69, 19);
        lblStatusApacheVer.Text = "Apache: —";
        //
        // lblStatusPhpUpdate
        //
        // Shown only while a PHP patch is available or being applied; click opens the version manager.
        //
        lblStatusPhpUpdate.IsLink = true;
        lblStatusPhpUpdate.Name = "lblStatusPhpUpdate";
        lblStatusPhpUpdate.Size = new Size(140, 19);
        lblStatusPhpUpdate.Text = "PHP update available";
        lblStatusPhpUpdate.Visible = false;
        lblStatusPhpUpdate.Click += btnPhpVersions_Click;
        // 
        // splitMain
        // 
        splitMain.Dock = DockStyle.Fill;
        splitMain.FixedPanel = FixedPanel.Panel1;
        splitMain.Location = new Point(0, 49);
        splitMain.Name = "splitMain";
        splitMain.Orientation = Orientation.Horizontal;
        // 
        // splitMain.Panel1
        // 
        splitMain.Panel1.Controls.Add(grpServices);
        splitMain.Panel1.Padding = new Padding(8, 6, 8, 3);
        splitMain.Panel1MinSize = 150;
        // 
        // splitMain.Panel2
        // 
        splitMain.Panel2.Controls.Add(grpLog);
        splitMain.Panel2.Padding = new Padding(8, 3, 8, 3);
        splitMain.Panel2MinSize = 120;
        splitMain.Size = new Size(884, 488);
        splitMain.SplitterDistance = 200;
        splitMain.SplitterWidth = 5;
        splitMain.TabIndex = 2;
        // 
        // grpServices
        // 
        grpServices.Controls.Add(lvServices);
        grpServices.Controls.Add(panSvcActions);
        grpServices.Dock = DockStyle.Fill;
        grpServices.Location = new Point(8, 6);
        grpServices.Name = "grpServices";
        grpServices.Padding = new Padding(8, 4, 8, 6);
        grpServices.Size = new Size(868, 191);
        grpServices.TabIndex = 0;
        grpServices.TabStop = false;
        grpServices.Text = "Services";
        // 
        // lvServices
        // 
        lvServices.Columns.AddRange(new ColumnHeader[] { colSvcModule, colSvcStatus, colSvcPid, colSvcPorts, colSvcVersion });
        lvServices.ContextMenuStrip = ctxServices;
        lvServices.Dock = DockStyle.Fill;
        lvServices.FullRowSelect = true;
        lvServices.GridLines = true;
        lvServices.Location = new Point(8, 20);
        lvServices.MultiSelect = false;
        lvServices.Name = "lvServices";
        lvServices.OwnerDraw = true;
        lvServices.Size = new Size(852, 127);
        lvServices.TabIndex = 0;
        lvServices.UseCompatibleStateImageBehavior = false;
        lvServices.View = View.Details;
        lvServices.DrawColumnHeader += lvServices_DrawColumnHeader;
        lvServices.DrawItem += lvServices_DrawItem;
        lvServices.DrawSubItem += lvServices_DrawSubItem;
        lvServices.SelectedIndexChanged += lvServices_SelectedIndexChanged;
        lvServices.DoubleClick += lvServices_DoubleClick;
        lvServices.Resize += lvServices_Resize;
        // 
        // colSvcModule
        // 
        colSvcModule.Text = "Module";
        colSvcModule.Width = 120;
        // 
        // colSvcStatus
        // 
        colSvcStatus.Text = "Status";
        colSvcStatus.Width = 110;
        // 
        // colSvcPid
        // 
        colSvcPid.Text = "PID";
        colSvcPid.TextAlign = HorizontalAlignment.Right;
        colSvcPid.Width = 70;
        // 
        // colSvcPorts
        // 
        colSvcPorts.Text = "Port(s)";
        colSvcPorts.Width = 190;
        // 
        // colSvcVersion
        // 
        colSvcVersion.Text = "Version";
        colSvcVersion.Width = 140;
        // 
        // ctxServices
        // 
        ctxServices.Items.AddRange(new ToolStripItem[] { ctxSvcStart, ctxSvcStop, ctxSvcSep1, ctxSvcConfig });
        ctxServices.Name = "ctxServices";
        ctxServices.Size = new Size(137, 76);
        ctxServices.Opening += ctxServices_Opening;
        // 
        // ctxSvcStart
        // 
        ctxSvcStart.Name = "ctxSvcStart";
        ctxSvcStart.Size = new Size(136, 22);
        ctxSvcStart.Text = "&Start";
        ctxSvcStart.Click += btnSvcStart_Click;
        // 
        // ctxSvcStop
        // 
        ctxSvcStop.Name = "ctxSvcStop";
        ctxSvcStop.Size = new Size(136, 22);
        ctxSvcStop.Text = "S&top";
        ctxSvcStop.Click += btnSvcStop_Click;
        // 
        // ctxSvcSep1
        // 
        ctxSvcSep1.Name = "ctxSvcSep1";
        ctxSvcSep1.Size = new Size(133, 6);
        // 
        // ctxSvcConfig
        // 
        ctxSvcConfig.Name = "ctxSvcConfig";
        ctxSvcConfig.Size = new Size(136, 22);
        ctxSvcConfig.Text = "&Configure…";
        ctxSvcConfig.Click += btnSvcConfig_Click;
        // 
        // panSvcActions
        // 
        panSvcActions.Controls.Add(flpSvcRight);
        panSvcActions.Controls.Add(flpSvcLeft);
        panSvcActions.Dock = DockStyle.Bottom;
        panSvcActions.Location = new Point(8, 147);
        panSvcActions.Name = "panSvcActions";
        panSvcActions.Padding = new Padding(0, 5, 0, 0);
        panSvcActions.Size = new Size(852, 38);
        panSvcActions.TabIndex = 1;
        // 
        // flpSvcRight
        // 
        flpSvcRight.Controls.Add(btnSvcStart);
        flpSvcRight.Controls.Add(btnSvcStop);
        flpSvcRight.Controls.Add(btnSvcConfig);
        flpSvcRight.Dock = DockStyle.Right;
        flpSvcRight.Location = new Point(569, 5);
        flpSvcRight.Name = "flpSvcRight";
        flpSvcRight.Size = new Size(360, 33);
        flpSvcRight.TabIndex = 1;
        flpSvcRight.WrapContents = false;
        // 
        // btnSvcStart
        // 
        btnSvcStart.Location = new Point(3, 3);
        btnSvcStart.Name = "btnSvcStart";
        btnSvcStart.Size = new Size(85, 25);
        btnSvcStart.TabIndex = 0;
        btnSvcStart.Text = "&Start";
        btnSvcStart.UseVisualStyleBackColor = true;
        btnSvcStart.Click += btnSvcStart_Click;
        // 
        // btnSvcStop
        // 
        btnSvcStop.Location = new Point(94, 3);
        btnSvcStop.Name = "btnSvcStop";
        btnSvcStop.Size = new Size(85, 25);
        btnSvcStop.TabIndex = 1;
        btnSvcStop.Text = "Sto&p";
        btnSvcStop.UseVisualStyleBackColor = true;
        btnSvcStop.Click += btnSvcStop_Click;
        // 
        // btnSvcConfig
        // 
        btnSvcConfig.Location = new Point(185, 3);
        btnSvcConfig.Name = "btnSvcConfig";
        btnSvcConfig.Size = new Size(90, 25);
        btnSvcConfig.TabIndex = 2;
        btnSvcConfig.Text = "&Configure…";
        btnSvcConfig.UseVisualStyleBackColor = true;
        btnSvcConfig.Click += btnSvcConfig_Click;
        // 
        // flpSvcLeft
        // 
        flpSvcLeft.Controls.Add(lblPhpVersionCap);
        flpSvcLeft.Controls.Add(cmbPhpVersion);
        flpSvcLeft.Controls.Add(btnPhpVersions);
        flpSvcLeft.Dock = DockStyle.Left;
        flpSvcLeft.Location = new Point(0, 5);
        flpSvcLeft.Name = "flpSvcLeft";
        flpSvcLeft.Size = new Size(310, 33);
        flpSvcLeft.TabIndex = 0;
        flpSvcLeft.WrapContents = false;
        // 
        // lblPhpVersionCap
        // 
        lblPhpVersionCap.AutoSize = true;
        lblPhpVersionCap.Location = new Point(3, 7);
        lblPhpVersionCap.Margin = new Padding(3, 7, 3, 0);
        lblPhpVersionCap.Name = "lblPhpVersionCap";
        lblPhpVersionCap.Size = new Size(74, 15);
        lblPhpVersionCap.TabIndex = 0;
        lblPhpVersionCap.Text = "PHP version:";
        // 
        // cmbPhpVersion
        // 
        cmbPhpVersion.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPhpVersion.FormattingEnabled = true;
        cmbPhpVersion.Location = new Point(83, 3);
        cmbPhpVersion.Name = "cmbPhpVersion";
        cmbPhpVersion.Size = new Size(130, 23);
        cmbPhpVersion.TabIndex = 1;
        cmbPhpVersion.SelectedIndexChanged += cmbPhpVersion_SelectedIndexChanged;
        // 
        // btnPhpVersions
        // 
        btnPhpVersions.Location = new Point(219, 3);
        btnPhpVersions.Name = "btnPhpVersions";
        btnPhpVersions.Size = new Size(80, 25);
        btnPhpVersions.TabIndex = 2;
        btnPhpVersions.Text = "Manage…";
        btnPhpVersions.UseVisualStyleBackColor = true;
        btnPhpVersions.Click += btnPhpVersions_Click;
        // 
        // grpLog
        // 
        grpLog.Controls.Add(rtbLog);
        grpLog.Controls.Add(tsLog);
        grpLog.Dock = DockStyle.Fill;
        grpLog.Location = new Point(8, 3);
        grpLog.Name = "grpLog";
        grpLog.Padding = new Padding(8, 4, 8, 8);
        grpLog.Size = new Size(868, 277);
        grpLog.TabIndex = 0;
        grpLog.TabStop = false;
        grpLog.Text = "Log";
        // 
        // rtbLog
        // 
        rtbLog.BackColor = SystemColors.Window;
        rtbLog.Dock = DockStyle.Fill;
        rtbLog.HideSelection = false;
        rtbLog.Location = new Point(8, 45);
        rtbLog.Name = "rtbLog";
        rtbLog.ReadOnly = true;
        rtbLog.Size = new Size(852, 224);
        rtbLog.TabIndex = 1;
        rtbLog.Text = "";
        rtbLog.WordWrap = false;
        // 
        // tsLog
        // 
        tsLog.GripStyle = ToolStripGripStyle.Hidden;
        tsLog.Items.AddRange(new ToolStripItem[] { tslLogFilter, cmbLogFilter, tsLogSep1, btnClearLog, btnOpenLog });
        tsLog.Location = new Point(8, 20);
        tsLog.Name = "tsLog";
        tsLog.Size = new Size(852, 25);
        tsLog.TabIndex = 0;
        // 
        // tslLogFilter
        // 
        tslLogFilter.Name = "tslLogFilter";
        tslLogFilter.Size = new Size(36, 22);
        tslLogFilter.Text = "Filter:";
        // 
        // cmbLogFilter
        // 
        cmbLogFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLogFilter.Items.AddRange(new object[] { "All", "Apache", "PHP", "Error" });
        cmbLogFilter.Name = "cmbLogFilter";
        cmbLogFilter.Size = new Size(110, 25);
        cmbLogFilter.ToolTipText = "Filter log entries by service or severity";
        cmbLogFilter.SelectedIndexChanged += cmbLogFilter_SelectedIndexChanged;
        // 
        // tsLogSep1
        // 
        tsLogSep1.Name = "tsLogSep1";
        tsLogSep1.Size = new Size(6, 25);
        // 
        // btnClearLog
        // 
        btnClearLog.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new Size(38, 22);
        btnClearLog.Text = "Clear";
        btnClearLog.ToolTipText = "Clear all log output";
        btnClearLog.Click += btnClearLog_Click;
        // 
        // btnOpenLog
        // 
        btnOpenLog.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnOpenLog.Name = "btnOpenLog";
        btnOpenLog.Size = new Size(76, 22);
        btnOpenLog.Text = "Open Folder";
        btnOpenLog.ToolTipText = "Open the server log folder in Explorer";
        btnOpenLog.Click += btnOpenLog_Click;
        // 
        // HamsterServerForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(884, 561);
        Controls.Add(splitMain);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Controls.Add(menuStrip);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = menuStrip;
        MinimumSize = new Size(720, 480);
        Name = "HamsterServerForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Hamster Server Control Panel";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        grpServices.ResumeLayout(false);
        ctxServices.ResumeLayout(false);
        panSvcActions.ResumeLayout(false);
        flpSvcRight.ResumeLayout(false);
        flpSvcLeft.ResumeLayout(false);
        flpSvcLeft.PerformLayout();
        grpLog.ResumeLayout(false);
        grpLog.PerformLayout();
        tsLog.ResumeLayout(false);
        tsLog.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private MenuStrip menuStrip;
    private ToolStripMenuItem mnuFile;
    private ToolStripMenuItem mnuFileOpenWww;
    private ToolStripMenuItem mnuFileOpenLogs;
    private ToolStripSeparator mnuFileSep1;
    private ToolStripMenuItem mnuFileHide;
    private ToolStripMenuItem mnuFileExit;
    private ToolStripMenuItem mnuService;
    private ToolStripMenuItem mnuSvcStartAll;
    private ToolStripMenuItem mnuSvcStopAll;
    private ToolStripMenuItem mnuSvcRestartAll;
    private ToolStripSeparator mnuSvcSep1;
    private ToolStripMenuItem mnuSvcApache;
    private ToolStripMenuItem mnuSvcApacheStart;
    private ToolStripMenuItem mnuSvcApacheStop;
    private ToolStripMenuItem mnuSvcPhp;
    private ToolStripMenuItem mnuSvcPhpStart;
    private ToolStripMenuItem mnuSvcPhpStop;
    private ToolStripMenuItem mnuTools;
    private ToolStripMenuItem mnuToolsSettings;
    private ToolStripMenuItem mnuToolsPhpVersions;
    private ToolStripMenuItem mnuToolsVHosts;
    private ToolStripMenuItem mnuToolsPorts;
    private ToolStripSeparator mnuToolsSep1;
    private ToolStripMenuItem mnuToolsApacheConfig;
    private ToolStripMenuItem mnuToolsPhpConfig;
    private ToolStripSeparator mnuToolsSep2;
    private ToolStripMenuItem mnuToolsBrowser;
    private ToolStripMenuItem mnuHelp;
    private ToolStripMenuItem mnuHelpAbout;

    private ToolStrip toolStrip;
    private ToolStripButton tsbStartAll;
    private ToolStripButton tsbStopAll;
    private ToolStripButton tsbRestart;
    private ToolStripSeparator tsbSep1;
    private ToolStripButton tsbSettings;
    private ToolStripButton tsbVHosts;
    private ToolStripButton tsbPorts;
    private ToolStripSeparator tsbSep2;
    private ToolStripButton tsbOpenWww;
    private ToolStripButton tsbBrowser;

    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblGlobalStatus;
    private ToolStripStatusLabel lblStatusSpring;
    private ToolStripStatusLabel lblStatusMode;
    private ToolStripStatusLabel lblStatusPhpVer;
    private ToolStripStatusLabel lblStatusApacheVer;
    private ToolStripStatusLabel lblStatusPhpUpdate;

    private SplitContainer splitMain;
    private GroupBox grpServices;
    private ListView lvServices;
    private ColumnHeader colSvcModule;
    private ColumnHeader colSvcStatus;
    private ColumnHeader colSvcPid;
    private ColumnHeader colSvcPorts;
    private ColumnHeader colSvcVersion;
    private ContextMenuStrip ctxServices;
    private ToolStripMenuItem ctxSvcStart;
    private ToolStripMenuItem ctxSvcStop;
    private ToolStripSeparator ctxSvcSep1;
    private ToolStripMenuItem ctxSvcConfig;
    private Panel panSvcActions;
    private FlowLayoutPanel flpSvcRight;
    private Button btnSvcStart;
    private Button btnSvcStop;
    private Button btnSvcConfig;
    private FlowLayoutPanel flpSvcLeft;
    private Label lblPhpVersionCap;
    private ComboBox cmbPhpVersion;
    private Button btnPhpVersions;

    private GroupBox grpLog;
    private RichTextBox rtbLog;
    private ToolStrip tsLog;
    private ToolStripLabel tslLogFilter;
    private ToolStripComboBox cmbLogFilter;
    private ToolStripSeparator tsLogSep1;
    private ToolStripButton btnClearLog;
    private ToolStripButton btnOpenLog;
}
