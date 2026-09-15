namespace HamsterServer.Forms;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        tabControl = new TabControl();
        tabApache = new TabPage();
        tblApacheRoot = new TableLayoutPanel();
        grpApacheServer = new GroupBox();
        tblApacheServer = new TableLayoutPanel();
        lblCapServerName = new Label();
        txtServerName = new TextBox();
        lblCapMaxClients = new Label();
        nudMaxClients = new NumericUpDown();
        lblCapKeepAlive = new Label();
        nudKeepAlive = new NumericUpDown();
        grpApacheCompression = new GroupBox();
        flpCompression = new FlowLayoutPanel();
        chkGzip = new CheckBox();
        chkBrotli = new CheckBox();
        chkCache = new CheckBox();
        grpApacheSsl = new GroupBox();
        tblApacheSsl = new TableLayoutPanel();
        chkSsl = new CheckBox();
        lblCapSslCert = new Label();
        txtSslCertFile = new TextBox();
        btnBrowseSslCert = new Button();
        lblCapSslKey = new Label();
        txtSslKeyFile = new TextBox();
        btnBrowseSslKey = new Button();

        tabPhp = new TabPage();
        tblPhpRoot = new TableLayoutPanel();
        grpPhpFastCgi = new GroupBox();
        tblPhpFastCgi = new TableLayoutPanel();
        lblCapFcgiPort = new Label();
        nudFcgiPort = new NumericUpDown();
        lblCapWorkers = new Label();
        nudWorkers = new NumericUpDown();
        lblCapMaxRequests = new Label();
        nudMaxRequests = new NumericUpDown();
        lblCapMemoryRestart = new Label();
        nudMemoryRestart = new NumericUpDown();
        lblMemoryWarning = new Label();
        grpPhpLimits = new GroupBox();
        tblPhpLimits = new TableLayoutPanel();
        lblCapMemoryLimit = new Label();
        nudMemoryLimit = new NumericUpDown();
        lblCapMaxExecTime = new Label();
        nudMaxExecTime = new NumericUpDown();
        lblCapTimezone = new Label();
        cboTimezone = new ComboBox();
        lblCapCaCertificate = new Label();
        chkUseBundledCaCertificate = new CheckBox();
        grpPhpOpcache = new GroupBox();
        tblPhpOpcache = new TableLayoutPanel();
        chkOpcache = new CheckBox();
        lblCapOpcacheMem = new Label();
        nudOpcacheMemory = new NumericUpDown();
        grpPhpExtensions = new GroupBox();
        clbExtensions = new CheckedListBox();
        panExtFooter = new Panel();
        lblExtInfo = new Label();
        flpExtActions = new FlowLayoutPanel();
        btnRecommendedExtensions = new Button();
        btnCheckAll = new Button();
        btnUncheckAll = new Button();

        tabGeneral = new TabPage();
        tblGeneralRoot = new TableLayoutPanel();
        grpMode = new GroupBox();
        tblMode = new TableLayoutPanel();
        lblCapMode = new Label();
        cboServerMode = new ComboBox();
        btnApplyModeDefaults = new Button();
        grpStartup = new GroupBox();
        flpStartup = new FlowLayoutPanel();
        chkMinimizeToTray = new CheckBox();
        chkAutoStart = new CheckBox();
        chkStartWithWindows = new CheckBox();
        grpSecurity = new GroupBox();
        flpSecurity = new FlowLayoutPanel();
        chkAllowLanAccess = new CheckBox();

        panButtons = new Panel();
        flpButtons = new FlowLayoutPanel();
        btnCancel = new Button();
        btnSave = new Button();

        tabControl.SuspendLayout();
        tabApache.SuspendLayout();
        tblApacheRoot.SuspendLayout();
        grpApacheServer.SuspendLayout();
        tblApacheServer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudMaxClients).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudKeepAlive).BeginInit();
        grpApacheCompression.SuspendLayout();
        flpCompression.SuspendLayout();
        grpApacheSsl.SuspendLayout();
        tblApacheSsl.SuspendLayout();
        tabPhp.SuspendLayout();
        tblPhpRoot.SuspendLayout();
        grpPhpFastCgi.SuspendLayout();
        tblPhpFastCgi.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudFcgiPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudWorkers).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxRequests).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMemoryRestart).BeginInit();
        grpPhpLimits.SuspendLayout();
        tblPhpLimits.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudMemoryLimit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxExecTime).BeginInit();
        grpPhpOpcache.SuspendLayout();
        tblPhpOpcache.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudOpcacheMemory).BeginInit();
        grpPhpExtensions.SuspendLayout();
        panExtFooter.SuspendLayout();
        flpExtActions.SuspendLayout();
        tabGeneral.SuspendLayout();
        tblGeneralRoot.SuspendLayout();
        grpMode.SuspendLayout();
        tblMode.SuspendLayout();
        grpStartup.SuspendLayout();
        flpStartup.SuspendLayout();
        grpSecurity.SuspendLayout();
        flpSecurity.SuspendLayout();
        panButtons.SuspendLayout();
        flpButtons.SuspendLayout();
        SuspendLayout();

        //
        // tabControl
        //
        tabControl.Controls.Add(tabApache);
        tabControl.Controls.Add(tabPhp);
        tabControl.Controls.Add(tabGeneral);
        tabControl.Dock = DockStyle.Fill;
        tabControl.Location = new Point(10, 10);
        tabControl.Name = "tabControl";
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(604, 620);
        tabControl.TabIndex = 0;

        // ═══ Apache tab ═══════════════════════════════════════════════════════
        //
        // tabApache
        //
        tabApache.Controls.Add(tblApacheRoot);
        tabApache.Location = new Point(4, 24);
        tabApache.Name = "tabApache";
        tabApache.Padding = new Padding(10);
        tabApache.Size = new Size(596, 562);
        tabApache.TabIndex = 0;
        tabApache.Text = "Apache";
        tabApache.UseVisualStyleBackColor = true;
        //
        // tblApacheRoot
        //
        tblApacheRoot.ColumnCount = 1;
        tblApacheRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblApacheRoot.Controls.Add(grpApacheServer, 0, 0);
        tblApacheRoot.Controls.Add(grpApacheCompression, 0, 1);
        tblApacheRoot.Controls.Add(grpApacheSsl, 0, 2);
        tblApacheRoot.Dock = DockStyle.Fill;
        tblApacheRoot.Location = new Point(10, 10);
        tblApacheRoot.Name = "tblApacheRoot";
        tblApacheRoot.RowCount = 4;
        tblApacheRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));
        tblApacheRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        tblApacheRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));
        tblApacheRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblApacheRoot.Size = new Size(576, 452);
        tblApacheRoot.TabIndex = 0;
        //
        // grpApacheServer
        //
        grpApacheServer.Controls.Add(tblApacheServer);
        grpApacheServer.Dock = DockStyle.Fill;
        grpApacheServer.Location = new Point(3, 3);
        grpApacheServer.Name = "grpApacheServer";
        grpApacheServer.Padding = new Padding(8, 4, 8, 6);
        grpApacheServer.Size = new Size(570, 116);
        grpApacheServer.TabIndex = 0;
        grpApacheServer.TabStop = false;
        grpApacheServer.Text = "Server";
        //
        // tblApacheServer
        //
        tblApacheServer.ColumnCount = 3;
        tblApacheServer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblApacheServer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblApacheServer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblApacheServer.Controls.Add(lblCapServerName, 0, 0);
        tblApacheServer.Controls.Add(txtServerName, 1, 0);
        tblApacheServer.Controls.Add(lblCapMaxClients, 0, 1);
        tblApacheServer.Controls.Add(nudMaxClients, 1, 1);
        tblApacheServer.Controls.Add(lblCapKeepAlive, 0, 2);
        tblApacheServer.Controls.Add(nudKeepAlive, 1, 2);
        tblApacheServer.Dock = DockStyle.Fill;
        tblApacheServer.Location = new Point(8, 20);
        tblApacheServer.Name = "tblApacheServer";
        tblApacheServer.RowCount = 3;
        tblApacheServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheServer.Size = new Size(554, 90);
        tblApacheServer.TabIndex = 0;
        //
        // lblCapServerName
        //
        lblCapServerName.AutoSize = true;
        lblCapServerName.Dock = DockStyle.Fill;
        lblCapServerName.Location = new Point(3, 0);
        lblCapServerName.Name = "lblCapServerName";
        lblCapServerName.Size = new Size(144, 30);
        lblCapServerName.TabIndex = 0;
        lblCapServerName.Text = "Server name:";
        lblCapServerName.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtServerName
        //
        txtServerName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtServerName.Location = new Point(153, 4);
        txtServerName.Name = "txtServerName";
        txtServerName.Size = new Size(308, 23);
        txtServerName.TabIndex = 1;
        //
        // lblCapMaxClients
        //
        lblCapMaxClients.AutoSize = true;
        lblCapMaxClients.Dock = DockStyle.Fill;
        lblCapMaxClients.Location = new Point(3, 30);
        lblCapMaxClients.Name = "lblCapMaxClients";
        lblCapMaxClients.Size = new Size(144, 30);
        lblCapMaxClients.TabIndex = 2;
        lblCapMaxClients.Text = "Max connections:";
        lblCapMaxClients.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMaxClients
        //
        nudMaxClients.Anchor = AnchorStyles.Left;
        nudMaxClients.Location = new Point(153, 34);
        nudMaxClients.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        nudMaxClients.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudMaxClients.Name = "nudMaxClients";
        nudMaxClients.Size = new Size(100, 23);
        nudMaxClients.TabIndex = 3;
        nudMaxClients.Value = new decimal(new int[] { 256, 0, 0, 0 });
        //
        // lblCapKeepAlive
        //
        lblCapKeepAlive.AutoSize = true;
        lblCapKeepAlive.Dock = DockStyle.Fill;
        lblCapKeepAlive.Location = new Point(3, 60);
        lblCapKeepAlive.Name = "lblCapKeepAlive";
        lblCapKeepAlive.Size = new Size(144, 30);
        lblCapKeepAlive.TabIndex = 4;
        lblCapKeepAlive.Text = "Keep-alive timeout (s):";
        lblCapKeepAlive.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudKeepAlive
        //
        nudKeepAlive.Anchor = AnchorStyles.Left;
        nudKeepAlive.Location = new Point(153, 64);
        nudKeepAlive.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        nudKeepAlive.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudKeepAlive.Name = "nudKeepAlive";
        nudKeepAlive.Size = new Size(100, 23);
        nudKeepAlive.TabIndex = 5;
        nudKeepAlive.Value = new decimal(new int[] { 15, 0, 0, 0 });
        //
        // grpApacheCompression
        //
        grpApacheCompression.Controls.Add(flpCompression);
        grpApacheCompression.Dock = DockStyle.Fill;
        grpApacheCompression.Location = new Point(3, 125);
        grpApacheCompression.Name = "grpApacheCompression";
        grpApacheCompression.Padding = new Padding(8, 4, 8, 6);
        grpApacheCompression.Size = new Size(570, 54);
        grpApacheCompression.TabIndex = 1;
        grpApacheCompression.TabStop = false;
        grpApacheCompression.Text = "Compression && cache";
        //
        // flpCompression
        //
        flpCompression.Controls.Add(chkGzip);
        flpCompression.Controls.Add(chkBrotli);
        flpCompression.Controls.Add(chkCache);
        flpCompression.Dock = DockStyle.Fill;
        flpCompression.Location = new Point(8, 20);
        flpCompression.Name = "flpCompression";
        flpCompression.Size = new Size(554, 28);
        flpCompression.TabIndex = 0;
        flpCompression.WrapContents = false;
        //
        // chkGzip
        //
        chkGzip.AutoSize = true;
        chkGzip.Location = new Point(3, 3);
        chkGzip.Margin = new Padding(3, 3, 20, 3);
        chkGzip.Name = "chkGzip";
        chkGzip.Size = new Size(105, 19);
        chkGzip.TabIndex = 0;
        chkGzip.Text = "Enable Gzip";
        chkGzip.UseVisualStyleBackColor = true;
        //
        // chkBrotli
        //
        chkBrotli.AutoSize = true;
        chkBrotli.Location = new Point(131, 3);
        chkBrotli.Margin = new Padding(3, 3, 20, 3);
        chkBrotli.Name = "chkBrotli";
        chkBrotli.Size = new Size(105, 19);
        chkBrotli.TabIndex = 1;
        chkBrotli.Text = "Enable Brotli";
        chkBrotli.UseVisualStyleBackColor = true;
        //
        // chkCache
        //
        chkCache.AutoSize = true;
        chkCache.Location = new Point(259, 3);
        chkCache.Name = "chkCache";
        chkCache.Size = new Size(150, 19);
        chkCache.TabIndex = 2;
        chkCache.Text = "Enable browser cache";
        chkCache.UseVisualStyleBackColor = true;
        //
        // grpApacheSsl
        //
        grpApacheSsl.Controls.Add(tblApacheSsl);
        grpApacheSsl.Dock = DockStyle.Fill;
        grpApacheSsl.Location = new Point(3, 185);
        grpApacheSsl.Name = "grpApacheSsl";
        grpApacheSsl.Padding = new Padding(8, 4, 8, 6);
        grpApacheSsl.Size = new Size(570, 116);
        grpApacheSsl.TabIndex = 2;
        grpApacheSsl.TabStop = false;
        grpApacheSsl.Text = "SSL";
        //
        // tblApacheSsl
        //
        tblApacheSsl.ColumnCount = 3;
        tblApacheSsl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblApacheSsl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblApacheSsl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblApacheSsl.Controls.Add(chkSsl, 0, 0);
        tblApacheSsl.Controls.Add(lblCapSslCert, 0, 1);
        tblApacheSsl.Controls.Add(txtSslCertFile, 1, 1);
        tblApacheSsl.Controls.Add(btnBrowseSslCert, 2, 1);
        tblApacheSsl.Controls.Add(lblCapSslKey, 0, 2);
        tblApacheSsl.Controls.Add(txtSslKeyFile, 1, 2);
        tblApacheSsl.Controls.Add(btnBrowseSslKey, 2, 2);
        tblApacheSsl.Dock = DockStyle.Fill;
        tblApacheSsl.Location = new Point(8, 20);
        tblApacheSsl.Name = "tblApacheSsl";
        tblApacheSsl.RowCount = 3;
        tblApacheSsl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheSsl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheSsl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblApacheSsl.Size = new Size(554, 90);
        tblApacheSsl.TabIndex = 0;
        //
        // chkSsl
        //
        chkSsl.Anchor = AnchorStyles.Left;
        chkSsl.AutoSize = true;
        tblApacheSsl.SetColumnSpan(chkSsl, 3);
        chkSsl.Location = new Point(3, 5);
        chkSsl.Name = "chkSsl";
        chkSsl.Size = new Size(180, 19);
        chkSsl.TabIndex = 0;
        chkSsl.Text = "Enable SSL (HTTPS)";
        chkSsl.UseVisualStyleBackColor = true;
        //
        // lblCapSslCert
        //
        lblCapSslCert.AutoSize = true;
        lblCapSslCert.Dock = DockStyle.Fill;
        lblCapSslCert.Location = new Point(3, 30);
        lblCapSslCert.Name = "lblCapSslCert";
        lblCapSslCert.Size = new Size(144, 30);
        lblCapSslCert.TabIndex = 1;
        lblCapSslCert.Text = "Certificate file:";
        lblCapSslCert.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtSslCertFile
        //
        txtSslCertFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtSslCertFile.Location = new Point(153, 34);
        txtSslCertFile.Name = "txtSslCertFile";
        txtSslCertFile.Size = new Size(308, 23);
        txtSslCertFile.TabIndex = 2;
        //
        // btnBrowseSslCert
        //
        btnBrowseSslCert.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnBrowseSslCert.Location = new Point(467, 33);
        btnBrowseSslCert.Name = "btnBrowseSslCert";
        btnBrowseSslCert.Size = new Size(84, 25);
        btnBrowseSslCert.TabIndex = 3;
        btnBrowseSslCert.Text = "Browse…";
        btnBrowseSslCert.UseVisualStyleBackColor = true;
        btnBrowseSslCert.Click += btnBrowseSslCert_Click;
        //
        // lblCapSslKey
        //
        lblCapSslKey.AutoSize = true;
        lblCapSslKey.Dock = DockStyle.Fill;
        lblCapSslKey.Location = new Point(3, 60);
        lblCapSslKey.Name = "lblCapSslKey";
        lblCapSslKey.Size = new Size(144, 30);
        lblCapSslKey.TabIndex = 4;
        lblCapSslKey.Text = "Private key file:";
        lblCapSslKey.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtSslKeyFile
        //
        txtSslKeyFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtSslKeyFile.Location = new Point(153, 64);
        txtSslKeyFile.Name = "txtSslKeyFile";
        txtSslKeyFile.Size = new Size(308, 23);
        txtSslKeyFile.TabIndex = 5;
        //
        // btnBrowseSslKey
        //
        btnBrowseSslKey.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnBrowseSslKey.Location = new Point(467, 63);
        btnBrowseSslKey.Name = "btnBrowseSslKey";
        btnBrowseSslKey.Size = new Size(84, 25);
        btnBrowseSslKey.TabIndex = 6;
        btnBrowseSslKey.Text = "Browse…";
        btnBrowseSslKey.UseVisualStyleBackColor = true;
        btnBrowseSslKey.Click += btnBrowseSslKey_Click;

        // ═══ PHP tab ══════════════════════════════════════════════════════════
        //
        // tabPhp
        //
        tabPhp.Controls.Add(tblPhpRoot);
        tabPhp.Location = new Point(4, 24);
        tabPhp.Name = "tabPhp";
        tabPhp.Padding = new Padding(10);
        tabPhp.Size = new Size(596, 592);
        tabPhp.TabIndex = 1;
        tabPhp.Text = "PHP";
        tabPhp.UseVisualStyleBackColor = true;
        //
        // tblPhpRoot
        //
        tblPhpRoot.ColumnCount = 1;
        tblPhpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblPhpRoot.Controls.Add(grpPhpFastCgi, 0, 0);
        tblPhpRoot.Controls.Add(grpPhpLimits, 0, 1);
        tblPhpRoot.Controls.Add(grpPhpOpcache, 0, 2);
        tblPhpRoot.Controls.Add(grpPhpExtensions, 0, 3);
        tblPhpRoot.Dock = DockStyle.Fill;
        tblPhpRoot.Location = new Point(10, 10);
        tblPhpRoot.Name = "tblPhpRoot";
        tblPhpRoot.RowCount = 4;
        tblPhpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
        tblPhpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 152F));
        tblPhpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        tblPhpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblPhpRoot.Size = new Size(576, 482);
        tblPhpRoot.TabIndex = 0;
        //
        // grpPhpFastCgi
        //
        grpPhpFastCgi.Controls.Add(tblPhpFastCgi);
        grpPhpFastCgi.Dock = DockStyle.Fill;
        grpPhpFastCgi.Location = new Point(3, 3);
        grpPhpFastCgi.Name = "grpPhpFastCgi";
        grpPhpFastCgi.Padding = new Padding(8, 4, 8, 6);
        grpPhpFastCgi.Size = new Size(570, 174);
        grpPhpFastCgi.TabIndex = 0;
        grpPhpFastCgi.TabStop = false;
        grpPhpFastCgi.Text = "FastCGI";
        //
        // tblPhpFastCgi
        //
        tblPhpFastCgi.ColumnCount = 2;
        tblPhpFastCgi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblPhpFastCgi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblPhpFastCgi.Controls.Add(lblCapFcgiPort, 0, 0);
        tblPhpFastCgi.Controls.Add(nudFcgiPort, 1, 0);
        tblPhpFastCgi.Controls.Add(lblCapWorkers, 0, 1);
        tblPhpFastCgi.Controls.Add(nudWorkers, 1, 1);
        tblPhpFastCgi.Controls.Add(lblCapMaxRequests, 0, 2);
        tblPhpFastCgi.Controls.Add(nudMaxRequests, 1, 2);
        tblPhpFastCgi.Controls.Add(lblCapMemoryRestart, 0, 3);
        tblPhpFastCgi.Controls.Add(nudMemoryRestart, 1, 3);
        tblPhpFastCgi.Controls.Add(lblMemoryWarning, 0, 4);
        tblPhpFastCgi.Dock = DockStyle.Fill;
        tblPhpFastCgi.Location = new Point(8, 20);
        tblPhpFastCgi.Name = "tblPhpFastCgi";
        tblPhpFastCgi.RowCount = 5;
        tblPhpFastCgi.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpFastCgi.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpFastCgi.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpFastCgi.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpFastCgi.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tblPhpFastCgi.Size = new Size(554, 148);
        tblPhpFastCgi.TabIndex = 0;
        //
        // lblCapMemoryRestart
        //
        lblCapMemoryRestart.AutoSize = true;
        lblCapMemoryRestart.Dock = DockStyle.Fill;
        lblCapMemoryRestart.Location = new Point(3, 90);
        lblCapMemoryRestart.Name = "lblCapMemoryRestart";
        lblCapMemoryRestart.Size = new Size(144, 30);
        lblCapMemoryRestart.TabIndex = 6;
        lblCapMemoryRestart.Text = "Restart pool above (MB):";
        lblCapMemoryRestart.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMemoryRestart
        //
        nudMemoryRestart.Anchor = AnchorStyles.Left;
        nudMemoryRestart.Increment = new decimal(new int[] { 128, 0, 0, 0 });
        nudMemoryRestart.Location = new Point(153, 94);
        nudMemoryRestart.Maximum = new decimal(new int[] { 65536, 0, 0, 0 });
        nudMemoryRestart.Name = "nudMemoryRestart";
        nudMemoryRestart.Size = new Size(100, 23);
        nudMemoryRestart.TabIndex = 7;
        //
        // lblMemoryWarning
        //
        tblPhpFastCgi.SetColumnSpan(lblMemoryWarning, 2);
        lblMemoryWarning.Dock = DockStyle.Fill;
        lblMemoryWarning.ForeColor = UiTheme.StatusTransition;
        lblMemoryWarning.Location = new Point(3, 120);
        lblMemoryWarning.Name = "lblMemoryWarning";
        lblMemoryWarning.Size = new Size(548, 28);
        lblMemoryWarning.TabIndex = 8;
        lblMemoryWarning.TextAlign = ContentAlignment.MiddleLeft;
        lblMemoryWarning.Visible = false;
        //
        // lblCapFcgiPort
        //
        lblCapFcgiPort.AutoSize = true;
        lblCapFcgiPort.Dock = DockStyle.Fill;
        lblCapFcgiPort.Location = new Point(3, 0);
        lblCapFcgiPort.Name = "lblCapFcgiPort";
        lblCapFcgiPort.Size = new Size(144, 30);
        lblCapFcgiPort.TabIndex = 0;
        lblCapFcgiPort.Text = "FastCGI port:";
        lblCapFcgiPort.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudFcgiPort
        //
        nudFcgiPort.Anchor = AnchorStyles.Left;
        nudFcgiPort.Location = new Point(153, 4);
        nudFcgiPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        nudFcgiPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudFcgiPort.Name = "nudFcgiPort";
        nudFcgiPort.Size = new Size(100, 23);
        nudFcgiPort.TabIndex = 1;
        nudFcgiPort.Value = new decimal(new int[] { 9000, 0, 0, 0 });
        //
        // lblCapWorkers
        //
        lblCapWorkers.AutoSize = true;
        lblCapWorkers.Dock = DockStyle.Fill;
        lblCapWorkers.Location = new Point(3, 30);
        lblCapWorkers.Name = "lblCapWorkers";
        lblCapWorkers.Size = new Size(144, 30);
        lblCapWorkers.TabIndex = 2;
        lblCapWorkers.Text = "Worker processes:";
        lblCapWorkers.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudWorkers
        //
        nudWorkers.Anchor = AnchorStyles.Left;
        nudWorkers.Location = new Point(153, 34);
        nudWorkers.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
        nudWorkers.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudWorkers.Name = "nudWorkers";
        nudWorkers.Size = new Size(100, 23);
        nudWorkers.TabIndex = 3;
        nudWorkers.Value = new decimal(new int[] { 8, 0, 0, 0 });
        //
        // lblCapMaxRequests
        //
        lblCapMaxRequests.AutoSize = true;
        lblCapMaxRequests.Dock = DockStyle.Fill;
        lblCapMaxRequests.Location = new Point(3, 60);
        lblCapMaxRequests.Name = "lblCapMaxRequests";
        lblCapMaxRequests.Size = new Size(144, 30);
        lblCapMaxRequests.TabIndex = 4;
        lblCapMaxRequests.Text = "Requests per worker:";
        lblCapMaxRequests.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMaxRequests
        //
        nudMaxRequests.Anchor = AnchorStyles.Left;
        nudMaxRequests.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        nudMaxRequests.Location = new Point(153, 64);
        nudMaxRequests.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
        nudMaxRequests.Name = "nudMaxRequests";
        nudMaxRequests.Size = new Size(100, 23);
        nudMaxRequests.TabIndex = 5;
        //
        // grpPhpLimits
        //
        grpPhpLimits.Controls.Add(tblPhpLimits);
        grpPhpLimits.Dock = DockStyle.Fill;
        grpPhpLimits.Location = new Point(3, 153);
        grpPhpLimits.Name = "grpPhpLimits";
        grpPhpLimits.Padding = new Padding(8, 4, 8, 6);
        grpPhpLimits.Size = new Size(570, 146);
        grpPhpLimits.TabIndex = 1;
        grpPhpLimits.TabStop = false;
        grpPhpLimits.Text = "Runtime";
        //
        // tblPhpLimits
        //
        tblPhpLimits.ColumnCount = 2;
        tblPhpLimits.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblPhpLimits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblPhpLimits.Controls.Add(lblCapMemoryLimit, 0, 0);
        tblPhpLimits.Controls.Add(nudMemoryLimit, 1, 0);
        tblPhpLimits.Controls.Add(lblCapMaxExecTime, 0, 1);
        tblPhpLimits.Controls.Add(nudMaxExecTime, 1, 1);
        tblPhpLimits.Controls.Add(lblCapTimezone, 0, 2);
        tblPhpLimits.Controls.Add(cboTimezone, 1, 2);
        tblPhpLimits.Controls.Add(lblCapCaCertificate, 0, 3);
        tblPhpLimits.Controls.Add(chkUseBundledCaCertificate, 1, 3);
        tblPhpLimits.Dock = DockStyle.Fill;
        tblPhpLimits.Location = new Point(8, 20);
        tblPhpLimits.Name = "tblPhpLimits";
        tblPhpLimits.RowCount = 4;
        tblPhpLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpLimits.Size = new Size(554, 120);
        tblPhpLimits.TabIndex = 0;
        //
        // lblCapMemoryLimit
        //
        lblCapMemoryLimit.AutoSize = true;
        lblCapMemoryLimit.Dock = DockStyle.Fill;
        lblCapMemoryLimit.Location = new Point(3, 0);
        lblCapMemoryLimit.Name = "lblCapMemoryLimit";
        lblCapMemoryLimit.Size = new Size(144, 30);
        lblCapMemoryLimit.TabIndex = 0;
        lblCapMemoryLimit.Text = "Memory limit (MB):";
        lblCapMemoryLimit.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMemoryLimit
        //
        nudMemoryLimit.Anchor = AnchorStyles.Left;
        nudMemoryLimit.Increment = new decimal(new int[] { 32, 0, 0, 0 });
        nudMemoryLimit.Location = new Point(153, 4);
        nudMemoryLimit.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        nudMemoryLimit.Minimum = new decimal(new int[] { 16, 0, 0, 0 });
        nudMemoryLimit.Name = "nudMemoryLimit";
        nudMemoryLimit.Size = new Size(100, 23);
        nudMemoryLimit.TabIndex = 1;
        nudMemoryLimit.Value = new decimal(new int[] { 512, 0, 0, 0 });
        //
        // lblCapMaxExecTime
        //
        lblCapMaxExecTime.AutoSize = true;
        lblCapMaxExecTime.Dock = DockStyle.Fill;
        lblCapMaxExecTime.Location = new Point(3, 30);
        lblCapMaxExecTime.Name = "lblCapMaxExecTime";
        lblCapMaxExecTime.Size = new Size(144, 30);
        lblCapMaxExecTime.TabIndex = 2;
        lblCapMaxExecTime.Text = "Max execution time (s):";
        lblCapMaxExecTime.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMaxExecTime
        //
        nudMaxExecTime.Anchor = AnchorStyles.Left;
        nudMaxExecTime.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        nudMaxExecTime.Location = new Point(153, 34);
        nudMaxExecTime.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
        nudMaxExecTime.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudMaxExecTime.Name = "nudMaxExecTime";
        nudMaxExecTime.Size = new Size(100, 23);
        nudMaxExecTime.TabIndex = 3;
        nudMaxExecTime.Value = new decimal(new int[] { 300, 0, 0, 0 });
        //
        // lblCapTimezone
        //
        lblCapTimezone.AutoSize = true;
        lblCapTimezone.Dock = DockStyle.Fill;
        lblCapTimezone.Location = new Point(3, 60);
        lblCapTimezone.Name = "lblCapTimezone";
        lblCapTimezone.Size = new Size(144, 30);
        lblCapTimezone.TabIndex = 4;
        lblCapTimezone.Text = "Timezone:";
        lblCapTimezone.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cboTimezone
        //
        cboTimezone.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        cboTimezone.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTimezone.FormattingEnabled = true;
        cboTimezone.Location = new Point(153, 63);
        cboTimezone.Name = "cboTimezone";
        cboTimezone.Size = new Size(398, 23);
        cboTimezone.TabIndex = 5;
        //
        // lblCapCaCertificate
        //
        lblCapCaCertificate.AutoSize = true;
        lblCapCaCertificate.Dock = DockStyle.Fill;
        lblCapCaCertificate.Location = new Point(3, 90);
        lblCapCaCertificate.Name = "lblCapCaCertificate";
        lblCapCaCertificate.Size = new Size(144, 30);
        lblCapCaCertificate.TabIndex = 6;
        lblCapCaCertificate.Text = "HTTPS CA certificate:";
        lblCapCaCertificate.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkUseBundledCaCertificate
        //
        chkUseBundledCaCertificate.Anchor = AnchorStyles.Left;
        chkUseBundledCaCertificate.AutoSize = true;
        chkUseBundledCaCertificate.Checked = true;
        chkUseBundledCaCertificate.CheckState = CheckState.Checked;
        chkUseBundledCaCertificate.Location = new Point(153, 95);
        chkUseBundledCaCertificate.Name = "chkUseBundledCaCertificate";
        chkUseBundledCaCertificate.Size = new Size(268, 19);
        chkUseBundledCaCertificate.TabIndex = 7;
        chkUseBundledCaCertificate.Text = "Use bundled ssl\\cacert.pem for API requests";
        chkUseBundledCaCertificate.UseVisualStyleBackColor = true;
        //
        // grpPhpOpcache
        //
        grpPhpOpcache.Controls.Add(tblPhpOpcache);
        grpPhpOpcache.Dock = DockStyle.Fill;
        grpPhpOpcache.Location = new Point(3, 305);
        grpPhpOpcache.Name = "grpPhpOpcache";
        grpPhpOpcache.Padding = new Padding(8, 4, 8, 6);
        grpPhpOpcache.Size = new Size(570, 56);
        grpPhpOpcache.TabIndex = 2;
        grpPhpOpcache.TabStop = false;
        grpPhpOpcache.Text = "OPcache";
        //
        // tblPhpOpcache
        //
        tblPhpOpcache.ColumnCount = 3;
        tblPhpOpcache.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblPhpOpcache.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblPhpOpcache.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblPhpOpcache.Controls.Add(chkOpcache, 0, 0);
        tblPhpOpcache.Controls.Add(lblCapOpcacheMem, 1, 0);
        tblPhpOpcache.Controls.Add(nudOpcacheMemory, 2, 0);
        tblPhpOpcache.Dock = DockStyle.Fill;
        tblPhpOpcache.Location = new Point(8, 20);
        tblPhpOpcache.Name = "tblPhpOpcache";
        tblPhpOpcache.RowCount = 1;
        tblPhpOpcache.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblPhpOpcache.Size = new Size(554, 30);
        tblPhpOpcache.TabIndex = 0;
        //
        // chkOpcache
        //
        chkOpcache.Anchor = AnchorStyles.Left;
        chkOpcache.AutoSize = true;
        chkOpcache.Location = new Point(3, 5);
        chkOpcache.Name = "chkOpcache";
        chkOpcache.Size = new Size(125, 19);
        chkOpcache.TabIndex = 0;
        chkOpcache.Text = "Enable OPcache";
        chkOpcache.UseVisualStyleBackColor = true;
        //
        // lblCapOpcacheMem
        //
        lblCapOpcacheMem.AutoSize = true;
        lblCapOpcacheMem.Dock = DockStyle.Fill;
        lblCapOpcacheMem.Location = new Point(153, 0);
        lblCapOpcacheMem.Name = "lblCapOpcacheMem";
        lblCapOpcacheMem.Size = new Size(144, 30);
        lblCapOpcacheMem.TabIndex = 1;
        lblCapOpcacheMem.Text = "Cache memory (MB):";
        lblCapOpcacheMem.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudOpcacheMemory
        //
        nudOpcacheMemory.Anchor = AnchorStyles.Left;
        nudOpcacheMemory.Increment = new decimal(new int[] { 32, 0, 0, 0 });
        nudOpcacheMemory.Location = new Point(303, 4);
        nudOpcacheMemory.Maximum = new decimal(new int[] { 4096, 0, 0, 0 });
        nudOpcacheMemory.Minimum = new decimal(new int[] { 16, 0, 0, 0 });
        nudOpcacheMemory.Name = "nudOpcacheMemory";
        nudOpcacheMemory.Size = new Size(100, 23);
        nudOpcacheMemory.TabIndex = 2;
        nudOpcacheMemory.Value = new decimal(new int[] { 256, 0, 0, 0 });
        //
        // grpPhpExtensions
        //
        grpPhpExtensions.Controls.Add(clbExtensions);
        grpPhpExtensions.Controls.Add(panExtFooter);
        grpPhpExtensions.Dock = DockStyle.Fill;
        grpPhpExtensions.Location = new Point(3, 367);
        grpPhpExtensions.Name = "grpPhpExtensions";
        grpPhpExtensions.Padding = new Padding(8, 4, 8, 6);
        grpPhpExtensions.Size = new Size(570, 110);
        grpPhpExtensions.TabIndex = 3;
        grpPhpExtensions.TabStop = false;
        grpPhpExtensions.Text = "Extensions";
        //
        // clbExtensions
        //
        clbExtensions.CheckOnClick = true;
        clbExtensions.ColumnWidth = 130;
        clbExtensions.Dock = DockStyle.Fill;
        clbExtensions.FormattingEnabled = true;
        clbExtensions.IntegralHeight = false;
        clbExtensions.Location = new Point(8, 20);
        clbExtensions.MultiColumn = true;
        clbExtensions.Name = "clbExtensions";
        clbExtensions.Size = new Size(554, 49);
        clbExtensions.TabIndex = 0;
        clbExtensions.ItemCheck += clbExtensions_ItemCheck;
        //
        // panExtFooter
        //
        panExtFooter.Controls.Add(lblExtInfo);
        panExtFooter.Controls.Add(flpExtActions);
        panExtFooter.Dock = DockStyle.Bottom;
        panExtFooter.Location = new Point(8, 69);
        panExtFooter.Name = "panExtFooter";
        panExtFooter.Padding = new Padding(0, 5, 0, 0);
        panExtFooter.Size = new Size(554, 35);
        panExtFooter.TabIndex = 1;
        //
        // lblExtInfo
        //
        lblExtInfo.Dock = DockStyle.Fill;
        lblExtInfo.ForeColor = SystemColors.GrayText;
        lblExtInfo.Location = new Point(0, 5);
        lblExtInfo.Name = "lblExtInfo";
        lblExtInfo.Size = new Size(254, 30);
        lblExtInfo.TabIndex = 0;
        lblExtInfo.Text = "Found: 0 extensions   Enabled: 0";
        lblExtInfo.TextAlign = ContentAlignment.MiddleLeft;
        //
        // flpExtActions
        //
        flpExtActions.Controls.Add(btnRecommendedExtensions);
        flpExtActions.Controls.Add(btnCheckAll);
        flpExtActions.Controls.Add(btnUncheckAll);
        flpExtActions.Dock = DockStyle.Right;
        flpExtActions.Location = new Point(254, 5);
        flpExtActions.Name = "flpExtActions";
        flpExtActions.Size = new Size(360, 30);
        flpExtActions.TabIndex = 1;
        flpExtActions.WrapContents = false;
        //
        // btnRecommendedExtensions
        //
        btnRecommendedExtensions.Location = new Point(3, 3);
        btnRecommendedExtensions.Name = "btnRecommendedExtensions";
        btnRecommendedExtensions.Size = new Size(96, 25);
        btnRecommendedExtensions.TabIndex = 0;
        btnRecommendedExtensions.Text = "Recommended";
        btnRecommendedExtensions.UseVisualStyleBackColor = true;
        btnRecommendedExtensions.Click += btnRecommendedExtensions_Click;
        //
        // btnCheckAll
        //
        btnCheckAll.Location = new Point(105, 3);
        btnCheckAll.Name = "btnCheckAll";
        btnCheckAll.Size = new Size(90, 25);
        btnCheckAll.TabIndex = 1;
        btnCheckAll.Text = "Check all";
        btnCheckAll.UseVisualStyleBackColor = true;
        btnCheckAll.Click += btnCheckAll_Click;
        //
        // btnUncheckAll
        //
        btnUncheckAll.Location = new Point(201, 3);
        btnUncheckAll.Name = "btnUncheckAll";
        btnUncheckAll.Size = new Size(90, 25);
        btnUncheckAll.TabIndex = 2;
        btnUncheckAll.Text = "Uncheck all";
        btnUncheckAll.UseVisualStyleBackColor = true;
        btnUncheckAll.Click += btnUncheckAll_Click;

        // ═══ General tab ══════════════════════════════════════════════════════
        //
        // tabGeneral
        //
        tabGeneral.Controls.Add(tblGeneralRoot);
        tabGeneral.Location = new Point(4, 24);
        tabGeneral.Name = "tabGeneral";
        tabGeneral.Padding = new Padding(10);
        tabGeneral.Size = new Size(596, 562);
        tabGeneral.TabIndex = 2;
        tabGeneral.Text = "General";
        tabGeneral.UseVisualStyleBackColor = true;
        //
        // tblGeneralRoot
        //
        tblGeneralRoot.ColumnCount = 1;
        tblGeneralRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblGeneralRoot.Controls.Add(grpMode, 0, 0);
        tblGeneralRoot.Controls.Add(grpStartup, 0, 1);
        tblGeneralRoot.Controls.Add(grpSecurity, 0, 2);
        tblGeneralRoot.Dock = DockStyle.Fill;
        tblGeneralRoot.Location = new Point(10, 10);
        tblGeneralRoot.Name = "tblGeneralRoot";
        tblGeneralRoot.RowCount = 4;
        tblGeneralRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        tblGeneralRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        tblGeneralRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        tblGeneralRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblGeneralRoot.Size = new Size(576, 452);
        tblGeneralRoot.TabIndex = 0;
        //
        // grpMode
        //
        grpMode.Controls.Add(tblMode);
        grpMode.Dock = DockStyle.Fill;
        grpMode.Location = new Point(3, 3);
        grpMode.Name = "grpMode";
        grpMode.Padding = new Padding(8, 4, 8, 6);
        grpMode.Size = new Size(570, 56);
        grpMode.TabIndex = 0;
        grpMode.TabStop = false;
        grpMode.Text = "Server mode";
        //
        // tblMode
        //
        tblMode.ColumnCount = 3;
        tblMode.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tblMode.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        tblMode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblMode.Controls.Add(lblCapMode, 0, 0);
        tblMode.Controls.Add(cboServerMode, 1, 0);
        tblMode.Controls.Add(btnApplyModeDefaults, 2, 0);
        tblMode.Dock = DockStyle.Fill;
        tblMode.Location = new Point(8, 20);
        tblMode.Name = "tblMode";
        tblMode.RowCount = 1;
        tblMode.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblMode.Size = new Size(554, 30);
        tblMode.TabIndex = 0;
        //
        // lblCapMode
        //
        lblCapMode.AutoSize = true;
        lblCapMode.Dock = DockStyle.Fill;
        lblCapMode.Location = new Point(3, 0);
        lblCapMode.Name = "lblCapMode";
        lblCapMode.Size = new Size(144, 30);
        lblCapMode.TabIndex = 0;
        lblCapMode.Text = "Mode:";
        lblCapMode.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cboServerMode
        //
        cboServerMode.Anchor = AnchorStyles.Left;
        cboServerMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cboServerMode.FormattingEnabled = true;
        cboServerMode.Location = new Point(153, 4);
        cboServerMode.Name = "cboServerMode";
        cboServerMode.Size = new Size(160, 23);
        cboServerMode.TabIndex = 1;
        //
        // btnApplyModeDefaults
        //
        btnApplyModeDefaults.Anchor = AnchorStyles.Left;
        btnApplyModeDefaults.Location = new Point(336, 3);
        btnApplyModeDefaults.Name = "btnApplyModeDefaults";
        btnApplyModeDefaults.Size = new Size(140, 25);
        btnApplyModeDefaults.TabIndex = 2;
        btnApplyModeDefaults.Text = "Apply mode defaults";
        btnApplyModeDefaults.UseVisualStyleBackColor = true;
        btnApplyModeDefaults.Click += btnApplyModeDefaults_Click;
        //
        // grpStartup
        //
        grpStartup.Controls.Add(flpStartup);
        grpStartup.Dock = DockStyle.Fill;
        grpStartup.Location = new Point(3, 65);
        grpStartup.Name = "grpStartup";
        grpStartup.Padding = new Padding(8, 4, 8, 6);
        grpStartup.Size = new Size(570, 104);
        grpStartup.TabIndex = 1;
        grpStartup.TabStop = false;
        grpStartup.Text = "Startup && window";
        //
        // flpStartup
        //
        flpStartup.Controls.Add(chkMinimizeToTray);
        flpStartup.Controls.Add(chkAutoStart);
        flpStartup.Controls.Add(chkStartWithWindows);
        flpStartup.Dock = DockStyle.Fill;
        flpStartup.FlowDirection = FlowDirection.TopDown;
        flpStartup.Location = new Point(8, 20);
        flpStartup.Name = "flpStartup";
        flpStartup.Size = new Size(554, 78);
        flpStartup.TabIndex = 0;
        flpStartup.WrapContents = false;
        //
        // chkMinimizeToTray
        //
        chkMinimizeToTray.AutoSize = true;
        chkMinimizeToTray.Location = new Point(3, 3);
        chkMinimizeToTray.Name = "chkMinimizeToTray";
        chkMinimizeToTray.Size = new Size(240, 19);
        chkMinimizeToTray.TabIndex = 0;
        chkMinimizeToTray.Text = "Close button minimizes to system tray";
        chkMinimizeToTray.UseVisualStyleBackColor = true;
        //
        // chkAutoStart
        //
        chkAutoStart.AutoSize = true;
        chkAutoStart.Location = new Point(3, 28);
        chkAutoStart.Name = "chkAutoStart";
        chkAutoStart.Size = new Size(240, 19);
        chkAutoStart.TabIndex = 1;
        chkAutoStart.Text = "Start services when Hamster Server opens";
        chkAutoStart.UseVisualStyleBackColor = true;
        //
        // chkStartWithWindows
        //
        chkStartWithWindows.AutoSize = true;
        chkStartWithWindows.Location = new Point(3, 53);
        chkStartWithWindows.Name = "chkStartWithWindows";
        chkStartWithWindows.Size = new Size(240, 19);
        chkStartWithWindows.TabIndex = 2;
        chkStartWithWindows.Text = "Launch Hamster Server when Windows starts";
        chkStartWithWindows.UseVisualStyleBackColor = true;
        //
        // grpSecurity
        //
        grpSecurity.Controls.Add(flpSecurity);
        grpSecurity.Dock = DockStyle.Fill;
        grpSecurity.Location = new Point(3, 175);
        grpSecurity.Name = "grpSecurity";
        grpSecurity.Padding = new Padding(8, 4, 8, 6);
        grpSecurity.Size = new Size(570, 56);
        grpSecurity.TabIndex = 2;
        grpSecurity.TabStop = false;
        grpSecurity.Text = "Network";
        //
        // flpSecurity
        //
        flpSecurity.Controls.Add(chkAllowLanAccess);
        flpSecurity.Dock = DockStyle.Fill;
        flpSecurity.FlowDirection = FlowDirection.TopDown;
        flpSecurity.Location = new Point(8, 20);
        flpSecurity.Name = "flpSecurity";
        flpSecurity.Size = new Size(554, 30);
        flpSecurity.TabIndex = 0;
        flpSecurity.WrapContents = false;
        //
        // chkAllowLanAccess
        //
        chkAllowLanAccess.AutoSize = true;
        chkAllowLanAccess.Location = new Point(3, 3);
        chkAllowLanAccess.Name = "chkAllowLanAccess";
        chkAllowLanAccess.Size = new Size(330, 19);
        chkAllowLanAccess.TabIndex = 0;
        chkAllowLanAccess.Text = "Allow access from other computers on the network (LAN)";
        chkAllowLanAccess.UseVisualStyleBackColor = true;

        // ═══ Dialog buttons ═══════════════════════════════════════════════════
        //
        // panButtons
        //
        panButtons.Controls.Add(flpButtons);
        panButtons.Dock = DockStyle.Bottom;
        panButtons.Location = new Point(10, 510);
        panButtons.Name = "panButtons";
        panButtons.Padding = new Padding(0, 8, 0, 0);
        panButtons.Size = new Size(604, 43);
        panButtons.TabIndex = 1;
        //
        // flpButtons
        //
        flpButtons.Controls.Add(btnCancel);
        flpButtons.Controls.Add(btnSave);
        flpButtons.Dock = DockStyle.Right;
        flpButtons.FlowDirection = FlowDirection.RightToLeft;
        flpButtons.Location = new Point(384, 8);
        flpButtons.Name = "flpButtons";
        flpButtons.Size = new Size(220, 35);
        flpButtons.TabIndex = 0;
        flpButtons.WrapContents = false;
        //
        // btnCancel
        //
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(127, 3);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 27);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        //
        // btnSave
        //
        btnSave.Location = new Point(31, 3);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(90, 27);
        btnSave.TabIndex = 0;
        btnSave.Text = "OK";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        //
        // SettingsForm
        //
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(624, 760);
        Controls.Add(tabControl);
        Controls.Add(panButtons);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 720);
        Name = "SettingsForm";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";

        tabControl.ResumeLayout(false);
        tabApache.ResumeLayout(false);
        tblApacheRoot.ResumeLayout(false);
        grpApacheServer.ResumeLayout(false);
        tblApacheServer.ResumeLayout(false);
        tblApacheServer.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudMaxClients).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudKeepAlive).EndInit();
        grpApacheCompression.ResumeLayout(false);
        flpCompression.ResumeLayout(false);
        flpCompression.PerformLayout();
        grpApacheSsl.ResumeLayout(false);
        tblApacheSsl.ResumeLayout(false);
        tblApacheSsl.PerformLayout();
        tabPhp.ResumeLayout(false);
        tblPhpRoot.ResumeLayout(false);
        grpPhpFastCgi.ResumeLayout(false);
        tblPhpFastCgi.ResumeLayout(false);
        tblPhpFastCgi.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudFcgiPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudWorkers).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxRequests).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMemoryRestart).EndInit();
        grpPhpLimits.ResumeLayout(false);
        tblPhpLimits.ResumeLayout(false);
        tblPhpLimits.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudMemoryLimit).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxExecTime).EndInit();
        grpPhpOpcache.ResumeLayout(false);
        tblPhpOpcache.ResumeLayout(false);
        tblPhpOpcache.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudOpcacheMemory).EndInit();
        grpPhpExtensions.ResumeLayout(false);
        panExtFooter.ResumeLayout(false);
        flpExtActions.ResumeLayout(false);
        tabGeneral.ResumeLayout(false);
        tblGeneralRoot.ResumeLayout(false);
        grpMode.ResumeLayout(false);
        tblMode.ResumeLayout(false);
        tblMode.PerformLayout();
        grpStartup.ResumeLayout(false);
        flpStartup.ResumeLayout(false);
        flpStartup.PerformLayout();
        grpSecurity.ResumeLayout(false);
        flpSecurity.ResumeLayout(false);
        flpSecurity.PerformLayout();
        panButtons.ResumeLayout(false);
        flpButtons.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TabControl tabControl;
    private TabPage tabApache;
    private TableLayoutPanel tblApacheRoot;
    private GroupBox grpApacheServer;
    private TableLayoutPanel tblApacheServer;
    private Label lblCapServerName;
    private TextBox txtServerName;
    private Label lblCapMaxClients;
    private NumericUpDown nudMaxClients;
    private Label lblCapKeepAlive;
    private NumericUpDown nudKeepAlive;
    private GroupBox grpApacheCompression;
    private FlowLayoutPanel flpCompression;
    private CheckBox chkGzip;
    private CheckBox chkBrotli;
    private CheckBox chkCache;
    private GroupBox grpApacheSsl;
    private TableLayoutPanel tblApacheSsl;
    private CheckBox chkSsl;
    private Label lblCapSslCert;
    private TextBox txtSslCertFile;
    private Button btnBrowseSslCert;
    private Label lblCapSslKey;
    private TextBox txtSslKeyFile;
    private Button btnBrowseSslKey;

    private TabPage tabPhp;
    private TableLayoutPanel tblPhpRoot;
    private GroupBox grpPhpFastCgi;
    private TableLayoutPanel tblPhpFastCgi;
    private Label lblCapFcgiPort;
    private NumericUpDown nudFcgiPort;
    private Label lblCapWorkers;
    private NumericUpDown nudWorkers;
    private Label lblCapMaxRequests;
    private NumericUpDown nudMaxRequests;
    private Label lblCapMemoryRestart;
    private NumericUpDown nudMemoryRestart;
    private Label lblMemoryWarning;
    private GroupBox grpPhpLimits;
    private TableLayoutPanel tblPhpLimits;
    private Label lblCapMemoryLimit;
    private NumericUpDown nudMemoryLimit;
    private Label lblCapMaxExecTime;
    private NumericUpDown nudMaxExecTime;
    private Label lblCapTimezone;
    private ComboBox cboTimezone;
    private Label lblCapCaCertificate;
    private CheckBox chkUseBundledCaCertificate;
    private GroupBox grpPhpOpcache;
    private TableLayoutPanel tblPhpOpcache;
    private CheckBox chkOpcache;
    private Label lblCapOpcacheMem;
    private NumericUpDown nudOpcacheMemory;
    private GroupBox grpPhpExtensions;
    private CheckedListBox clbExtensions;
    private Panel panExtFooter;
    private Label lblExtInfo;
    private FlowLayoutPanel flpExtActions;
    private Button btnRecommendedExtensions;
    private Button btnCheckAll;
    private Button btnUncheckAll;

    private TabPage tabGeneral;
    private TableLayoutPanel tblGeneralRoot;
    private GroupBox grpMode;
    private TableLayoutPanel tblMode;
    private Label lblCapMode;
    private ComboBox cboServerMode;
    private Button btnApplyModeDefaults;
    private GroupBox grpStartup;
    private FlowLayoutPanel flpStartup;
    private CheckBox chkMinimizeToTray;
    private CheckBox chkAutoStart;
    private CheckBox chkStartWithWindows;
    private GroupBox grpSecurity;
    private FlowLayoutPanel flpSecurity;
    private CheckBox chkAllowLanAccess;

    private Panel panButtons;
    private FlowLayoutPanel flpButtons;
    private Button btnCancel;
    private Button btnSave;
}
