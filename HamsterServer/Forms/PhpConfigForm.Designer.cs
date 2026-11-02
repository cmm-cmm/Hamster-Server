namespace HamsterServer.Forms;

partial class PhpConfigForm
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

        panPhpHeader = new Panel();
        lblPhpTitle = new Label();
        tabPhpConfig = new TabControl();
        tabSettings = new TabPage();
        tblSettings = new TableLayoutPanel();
        grpLimits = new GroupBox();
        tblLimits = new TableLayoutPanel();
        lblMemory = new Label();
        nudMemoryLimit = new NumericUpDown();
        lblUpload = new Label();
        nudUploadMax = new NumericUpDown();
        lblPostMax = new Label();
        nudPostMax = new NumericUpDown();
        lblExecTime = new Label();
        nudMaxExecTime = new NumericUpDown();
        grpErrors = new GroupBox();
        tblErrors = new TableLayoutPanel();
        lblErrorRep = new Label();
        cmbErrorReporting = new ComboBox();
        lblDisplayErrors = new Label();
        chkDisplayErrors = new CheckBox();
        lblShortTag = new Label();
        chkShortOpenTag = new CheckBox();
        lblTimezone = new Label();
        cboTimezone = new ComboBox();
        tabExtensions = new TabPage();
        clbExtensions = new CheckedListBox();
        tsExtensions = new ToolStrip();
        tslExtFilter = new ToolStripLabel();
        txtExtFilter = new ToolStripTextBox();
        tabViewIni = new TabPage();
        rtbPhpIni = new RichTextBox();
        tsIni = new ToolStrip();
        btnReloadIni = new ToolStripButton();
        tsIniSep1 = new ToolStripSeparator();
        lblIniHint = new ToolStripLabel();
        panPhpFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnPhpCancel = new Button();
        btnPhpSave = new Button();
        btnPhpSaveRestart = new Button();

        panPhpHeader.SuspendLayout();
        tabPhpConfig.SuspendLayout();
        tabSettings.SuspendLayout();
        tblSettings.SuspendLayout();
        grpLimits.SuspendLayout();
        tblLimits.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudMemoryLimit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudUploadMax).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudPostMax).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxExecTime).BeginInit();
        grpErrors.SuspendLayout();
        tblErrors.SuspendLayout();
        tabExtensions.SuspendLayout();
        tsExtensions.SuspendLayout();
        tabViewIni.SuspendLayout();
        tsIni.SuspendLayout();
        panPhpFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        SuspendLayout();

        //
        // panPhpHeader
        //
        panPhpHeader.Controls.Add(lblPhpTitle);
        panPhpHeader.Dock = DockStyle.Top;
        panPhpHeader.Location = new Point(10, 10);
        panPhpHeader.Name = "panPhpHeader";
        panPhpHeader.Padding = new Padding(0, 0, 0, 6);
        panPhpHeader.Size = new Size(744, 28);
        panPhpHeader.TabIndex = 0;
        //
        // lblPhpTitle
        //
        lblPhpTitle.Dock = DockStyle.Fill;
        lblPhpTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblPhpTitle.Location = new Point(0, 0);
        lblPhpTitle.Name = "lblPhpTitle";
        lblPhpTitle.Size = new Size(744, 22);
        lblPhpTitle.TabIndex = 0;
        lblPhpTitle.Text = "PHP Configuration";
        lblPhpTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // tabPhpConfig
        //
        tabPhpConfig.Controls.Add(tabSettings);
        tabPhpConfig.Controls.Add(tabExtensions);
        tabPhpConfig.Controls.Add(tabViewIni);
        tabPhpConfig.Dock = DockStyle.Fill;
        tabPhpConfig.Location = new Point(10, 38);
        tabPhpConfig.Name = "tabPhpConfig";
        tabPhpConfig.SelectedIndex = 0;
        tabPhpConfig.Size = new Size(744, 450);
        tabPhpConfig.TabIndex = 1;

        // ─── Settings tab ────────────────────────────────────────────────────
        //
        // tabSettings
        //
        tabSettings.Controls.Add(tblSettings);
        tabSettings.Location = new Point(4, 24);
        tabSettings.Name = "tabSettings";
        tabSettings.Padding = new Padding(10);
        tabSettings.Size = new Size(736, 422);
        tabSettings.TabIndex = 0;
        tabSettings.Text = "Settings";
        tabSettings.UseVisualStyleBackColor = true;
        //
        // tblSettings
        //
        tblSettings.ColumnCount = 1;
        tblSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblSettings.Controls.Add(grpLimits, 0, 0);
        tblSettings.Controls.Add(grpErrors, 0, 1);
        tblSettings.Dock = DockStyle.Fill;
        tblSettings.Location = new Point(10, 10);
        tblSettings.Name = "tblSettings";
        tblSettings.RowCount = 3;
        tblSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 152F));
        tblSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 152F));
        tblSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblSettings.Size = new Size(716, 402);
        tblSettings.TabIndex = 0;
        //
        // grpLimits
        //
        grpLimits.Controls.Add(tblLimits);
        grpLimits.Dock = DockStyle.Fill;
        grpLimits.Location = new Point(3, 3);
        grpLimits.Name = "grpLimits";
        grpLimits.Padding = new Padding(8, 4, 8, 6);
        grpLimits.Size = new Size(710, 146);
        grpLimits.TabIndex = 0;
        grpLimits.TabStop = false;
        grpLimits.Text = "Limits";
        //
        // tblLimits
        //
        tblLimits.ColumnCount = 2;
        tblLimits.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        tblLimits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblLimits.Controls.Add(lblMemory, 0, 0);
        tblLimits.Controls.Add(nudMemoryLimit, 1, 0);
        tblLimits.Controls.Add(lblUpload, 0, 1);
        tblLimits.Controls.Add(nudUploadMax, 1, 1);
        tblLimits.Controls.Add(lblPostMax, 0, 2);
        tblLimits.Controls.Add(nudPostMax, 1, 2);
        tblLimits.Controls.Add(lblExecTime, 0, 3);
        tblLimits.Controls.Add(nudMaxExecTime, 1, 3);
        tblLimits.Dock = DockStyle.Fill;
        tblLimits.Location = new Point(8, 20);
        tblLimits.Name = "tblLimits";
        tblLimits.RowCount = 4;
        tblLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblLimits.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblLimits.Size = new Size(694, 120);
        tblLimits.TabIndex = 0;
        //
        // lblMemory
        //
        lblMemory.AutoSize = true;
        lblMemory.Dock = DockStyle.Fill;
        lblMemory.Location = new Point(3, 0);
        lblMemory.Name = "lblMemory";
        lblMemory.Size = new Size(184, 30);
        lblMemory.TabIndex = 0;
        lblMemory.Text = "Memory limit (MB):";
        lblMemory.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMemoryLimit
        //
        nudMemoryLimit.Anchor = AnchorStyles.Left;
        nudMemoryLimit.Increment = new decimal(new int[] { 32, 0, 0, 0 });
        nudMemoryLimit.Location = new Point(193, 4);
        nudMemoryLimit.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        nudMemoryLimit.Minimum = new decimal(new int[] { 16, 0, 0, 0 });
        nudMemoryLimit.Name = "nudMemoryLimit";
        nudMemoryLimit.Size = new Size(100, 23);
        nudMemoryLimit.TabIndex = 1;
        nudMemoryLimit.Value = new decimal(new int[] { 512, 0, 0, 0 });
        //
        // lblUpload
        //
        lblUpload.AutoSize = true;
        lblUpload.Dock = DockStyle.Fill;
        lblUpload.Location = new Point(3, 30);
        lblUpload.Name = "lblUpload";
        lblUpload.Size = new Size(184, 30);
        lblUpload.TabIndex = 2;
        lblUpload.Text = "Max upload file size (MB):";
        lblUpload.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudUploadMax
        //
        nudUploadMax.Anchor = AnchorStyles.Left;
        nudUploadMax.Increment = new decimal(new int[] { 8, 0, 0, 0 });
        nudUploadMax.Location = new Point(193, 34);
        nudUploadMax.Maximum = new decimal(new int[] { 4096, 0, 0, 0 });
        nudUploadMax.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudUploadMax.Name = "nudUploadMax";
        nudUploadMax.Size = new Size(100, 23);
        nudUploadMax.TabIndex = 3;
        nudUploadMax.Value = new decimal(new int[] { 128, 0, 0, 0 });
        //
        // lblPostMax
        //
        lblPostMax.AutoSize = true;
        lblPostMax.Dock = DockStyle.Fill;
        lblPostMax.Location = new Point(3, 60);
        lblPostMax.Name = "lblPostMax";
        lblPostMax.Size = new Size(184, 30);
        lblPostMax.TabIndex = 4;
        lblPostMax.Text = "Max POST size (MB):";
        lblPostMax.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudPostMax
        //
        nudPostMax.Anchor = AnchorStyles.Left;
        nudPostMax.Increment = new decimal(new int[] { 8, 0, 0, 0 });
        nudPostMax.Location = new Point(193, 64);
        nudPostMax.Maximum = new decimal(new int[] { 4096, 0, 0, 0 });
        nudPostMax.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudPostMax.Name = "nudPostMax";
        nudPostMax.Size = new Size(100, 23);
        nudPostMax.TabIndex = 5;
        nudPostMax.Value = new decimal(new int[] { 128, 0, 0, 0 });
        //
        // lblExecTime
        //
        lblExecTime.AutoSize = true;
        lblExecTime.Dock = DockStyle.Fill;
        lblExecTime.Location = new Point(3, 90);
        lblExecTime.Name = "lblExecTime";
        lblExecTime.Size = new Size(184, 30);
        lblExecTime.TabIndex = 6;
        lblExecTime.Text = "Max execution time (s):";
        lblExecTime.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudMaxExecTime
        //
        nudMaxExecTime.Anchor = AnchorStyles.Left;
        nudMaxExecTime.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        nudMaxExecTime.Location = new Point(193, 94);
        nudMaxExecTime.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
        nudMaxExecTime.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudMaxExecTime.Name = "nudMaxExecTime";
        nudMaxExecTime.Size = new Size(100, 23);
        nudMaxExecTime.TabIndex = 7;
        nudMaxExecTime.Value = new decimal(new int[] { 300, 0, 0, 0 });
        //
        // grpErrors
        //
        grpErrors.Controls.Add(tblErrors);
        grpErrors.Dock = DockStyle.Fill;
        grpErrors.Location = new Point(3, 155);
        grpErrors.Name = "grpErrors";
        grpErrors.Padding = new Padding(8, 4, 8, 6);
        grpErrors.Size = new Size(710, 146);
        grpErrors.TabIndex = 1;
        grpErrors.TabStop = false;
        grpErrors.Text = "Errors && behavior";
        //
        // tblErrors
        //
        tblErrors.ColumnCount = 2;
        tblErrors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        tblErrors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblErrors.Controls.Add(lblErrorRep, 0, 0);
        tblErrors.Controls.Add(cmbErrorReporting, 1, 0);
        tblErrors.Controls.Add(lblDisplayErrors, 0, 1);
        tblErrors.Controls.Add(chkDisplayErrors, 1, 1);
        tblErrors.Controls.Add(lblShortTag, 0, 2);
        tblErrors.Controls.Add(chkShortOpenTag, 1, 2);
        tblErrors.Controls.Add(lblTimezone, 0, 3);
        tblErrors.Controls.Add(cboTimezone, 1, 3);
        tblErrors.Dock = DockStyle.Fill;
        tblErrors.Location = new Point(8, 20);
        tblErrors.Name = "tblErrors";
        tblErrors.RowCount = 4;
        tblErrors.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblErrors.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblErrors.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblErrors.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblErrors.Size = new Size(694, 120);
        tblErrors.TabIndex = 0;
        //
        // lblErrorRep
        //
        lblErrorRep.AutoSize = true;
        lblErrorRep.Dock = DockStyle.Fill;
        lblErrorRep.Location = new Point(3, 0);
        lblErrorRep.Name = "lblErrorRep";
        lblErrorRep.Size = new Size(184, 30);
        lblErrorRep.TabIndex = 0;
        lblErrorRep.Text = "Error reporting:";
        lblErrorRep.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cmbErrorReporting
        //
        cmbErrorReporting.Anchor = AnchorStyles.Left;
        cmbErrorReporting.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbErrorReporting.FormattingEnabled = true;
        cmbErrorReporting.Items.AddRange(new object[] {
            "Follow server mode", "E_ALL (all errors)", "E_ALL except deprecated", "E_ALL except notices" });
        cmbErrorReporting.Location = new Point(193, 4);
        cmbErrorReporting.Name = "cmbErrorReporting";
        cmbErrorReporting.Size = new Size(240, 23);
        cmbErrorReporting.TabIndex = 1;
        //
        // lblDisplayErrors
        //
        lblDisplayErrors.AutoSize = true;
        lblDisplayErrors.Dock = DockStyle.Fill;
        lblDisplayErrors.Location = new Point(3, 30);
        lblDisplayErrors.Name = "lblDisplayErrors";
        lblDisplayErrors.Size = new Size(184, 30);
        lblDisplayErrors.TabIndex = 2;
        lblDisplayErrors.Text = "Display errors:";
        lblDisplayErrors.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkDisplayErrors
        //
        chkDisplayErrors.Anchor = AnchorStyles.Left;
        chkDisplayErrors.AutoSize = true;
        chkDisplayErrors.Location = new Point(193, 35);
        chkDisplayErrors.Name = "chkDisplayErrors";
        chkDisplayErrors.Size = new Size(420, 19);
        chkDisplayErrors.TabIndex = 3;
        chkDisplayErrors.Text = "Always off — output before headers breaks FastCGI";
        chkDisplayErrors.UseVisualStyleBackColor = true;
        //
        // lblShortTag
        //
        lblShortTag.AutoSize = true;
        lblShortTag.Dock = DockStyle.Fill;
        lblShortTag.Location = new Point(3, 60);
        lblShortTag.Name = "lblShortTag";
        lblShortTag.Size = new Size(184, 30);
        lblShortTag.TabIndex = 4;
        lblShortTag.Text = "Short open tag:";
        lblShortTag.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkShortOpenTag
        //
        chkShortOpenTag.Anchor = AnchorStyles.Left;
        chkShortOpenTag.AutoSize = true;
        chkShortOpenTag.Location = new Point(193, 65);
        chkShortOpenTag.Name = "chkShortOpenTag";
        chkShortOpenTag.Size = new Size(300, 19);
        chkShortOpenTag.TabIndex = 5;
        chkShortOpenTag.Text = "Allow <? … ?> in addition to <?php";
        chkShortOpenTag.UseVisualStyleBackColor = true;
        //
        // lblTimezone
        //
        lblTimezone.AutoSize = true;
        lblTimezone.Dock = DockStyle.Fill;
        lblTimezone.Location = new Point(3, 90);
        lblTimezone.Name = "lblTimezone";
        lblTimezone.Size = new Size(184, 30);
        lblTimezone.TabIndex = 6;
        lblTimezone.Text = "Timezone:";
        lblTimezone.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cboTimezone
        //
        // Editable on purpose: the list is what Windows knows, and PHP accepts zones it does not.
        cboTimezone.Anchor = AnchorStyles.Left;
        cboTimezone.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        cboTimezone.AutoCompleteSource = AutoCompleteSource.ListItems;
        cboTimezone.DropDownStyle = ComboBoxStyle.DropDown;
        cboTimezone.FormattingEnabled = true;
        cboTimezone.Location = new Point(193, 94);
        cboTimezone.Name = "cboTimezone";
        cboTimezone.Size = new Size(300, 23);
        cboTimezone.TabIndex = 7;

        // ─── Extensions tab ──────────────────────────────────────────────────
        //
        // tabExtensions
        //
        tabExtensions.Controls.Add(clbExtensions);
        tabExtensions.Controls.Add(tsExtensions);
        tabExtensions.Location = new Point(4, 24);
        tabExtensions.Name = "tabExtensions";
        tabExtensions.Padding = new Padding(6);
        tabExtensions.Size = new Size(736, 422);
        tabExtensions.TabIndex = 1;
        tabExtensions.Text = "Extensions";
        tabExtensions.UseVisualStyleBackColor = true;
        //
        // clbExtensions
        //
        clbExtensions.CheckOnClick = true;
        clbExtensions.ColumnWidth = 150;
        clbExtensions.Dock = DockStyle.Fill;
        clbExtensions.FormattingEnabled = true;
        clbExtensions.IntegralHeight = false;
        clbExtensions.Location = new Point(6, 31);
        clbExtensions.MultiColumn = true;
        clbExtensions.Name = "clbExtensions";
        clbExtensions.Size = new Size(724, 385);
        clbExtensions.TabIndex = 1;
        clbExtensions.ItemCheck += clbExtensions_ItemCheck;
        //
        // tsExtensions
        //
        tsExtensions.Dock = DockStyle.Top;
        tsExtensions.GripStyle = ToolStripGripStyle.Hidden;
        tsExtensions.Items.AddRange(new ToolStripItem[] { tslExtFilter, txtExtFilter });
        tsExtensions.Location = new Point(6, 6);
        tsExtensions.Name = "tsExtensions";
        tsExtensions.Size = new Size(724, 25);
        tsExtensions.TabIndex = 0;
        //
        // tslExtFilter
        //
        tslExtFilter.Name = "tslExtFilter";
        tslExtFilter.Size = new Size(36, 22);
        tslExtFilter.Text = "Filter:";
        //
        // txtExtFilter
        //
        txtExtFilter.Name = "txtExtFilter";
        txtExtFilter.Size = new Size(220, 25);
        txtExtFilter.ToolTipText = "Type to narrow the extension list";
        txtExtFilter.TextChanged += txtExtFilter_TextChanged;

        // ─── php.ini tab ─────────────────────────────────────────────────────
        //
        // tabViewIni
        //
        tabViewIni.Controls.Add(rtbPhpIni);
        tabViewIni.Controls.Add(tsIni);
        tabViewIni.Location = new Point(4, 24);
        tabViewIni.Name = "tabViewIni";
        tabViewIni.Padding = new Padding(6);
        tabViewIni.Size = new Size(736, 422);
        tabViewIni.TabIndex = 2;
        tabViewIni.Text = "php.ini";
        tabViewIni.UseVisualStyleBackColor = true;
        //
        // rtbPhpIni
        //
        rtbPhpIni.BackColor = SystemColors.Window;
        rtbPhpIni.BorderStyle = BorderStyle.Fixed3D;
        rtbPhpIni.Dock = DockStyle.Fill;
        rtbPhpIni.Location = new Point(6, 31);
        rtbPhpIni.Name = "rtbPhpIni";
        rtbPhpIni.ReadOnly = true;
        rtbPhpIni.Size = new Size(724, 385);
        rtbPhpIni.TabIndex = 1;
        rtbPhpIni.Text = "";
        rtbPhpIni.WordWrap = false;
        //
        // tsIni
        //
        tsIni.Dock = DockStyle.Top;
        tsIni.GripStyle = ToolStripGripStyle.Hidden;
        tsIni.Items.AddRange(new ToolStripItem[] { btnReloadIni, tsIniSep1, lblIniHint });
        tsIni.Location = new Point(6, 6);
        tsIni.Name = "tsIni";
        tsIni.Size = new Size(724, 25);
        tsIni.TabIndex = 0;
        //
        // btnReloadIni
        //
        btnReloadIni.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnReloadIni.Name = "btnReloadIni";
        btnReloadIni.Size = new Size(52, 22);
        btnReloadIni.Text = "Reload";
        btnReloadIni.ToolTipText = "Re-read php.ini from disk";
        btnReloadIni.Click += btnReloadIni_Click;
        //
        // tsIniSep1
        //
        tsIniSep1.Name = "tsIniSep1";
        tsIniSep1.Size = new Size(6, 25);
        //
        // lblIniHint
        //
        lblIniHint.ForeColor = SystemColors.GrayText;
        lblIniHint.Name = "lblIniHint";
        lblIniHint.Size = new Size(420, 22);
        lblIniHint.Text = "Read-only preview. Saving regenerates php.ini for every runtime from these settings.";
        //
        // panPhpFooter
        //
        panPhpFooter.Controls.Add(flpFooter);
        panPhpFooter.Dock = DockStyle.Bottom;
        panPhpFooter.Location = new Point(10, 488);
        panPhpFooter.Name = "panPhpFooter";
        panPhpFooter.Padding = new Padding(0, 8, 0, 0);
        panPhpFooter.Size = new Size(744, 43);
        panPhpFooter.TabIndex = 2;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnPhpCancel);
        flpFooter.Controls.Add(btnPhpSave);
        flpFooter.Controls.Add(btnPhpSaveRestart);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(414, 8);
        flpFooter.Name = "flpFooter";
        flpFooter.Size = new Size(370, 35);
        flpFooter.TabIndex = 0;
        flpFooter.WrapContents = false;
        //
        // btnPhpCancel
        //
        btnPhpCancel.DialogResult = DialogResult.Cancel;
        btnPhpCancel.Location = new Point(237, 3);
        btnPhpCancel.Name = "btnPhpCancel";
        btnPhpCancel.Size = new Size(90, 27);
        btnPhpCancel.TabIndex = 2;
        btnPhpCancel.Text = "Close";
        btnPhpCancel.UseVisualStyleBackColor = true;
        btnPhpCancel.Click += btnPhpCancel_Click;
        //
        // btnPhpSave
        //
        btnPhpSave.Location = new Point(141, 3);
        btnPhpSave.Name = "btnPhpSave";
        btnPhpSave.Size = new Size(90, 27);
        btnPhpSave.TabIndex = 1;
        btnPhpSave.Text = "Save";
        btnPhpSave.UseVisualStyleBackColor = true;
        btnPhpSave.Click += btnPhpSave_Click;
        //
        // btnPhpSaveRestart
        //
        btnPhpSaveRestart.Location = new Point(5, 3);
        btnPhpSaveRestart.Name = "btnPhpSaveRestart";
        btnPhpSaveRestart.Size = new Size(130, 27);
        btnPhpSaveRestart.TabIndex = 0;
        btnPhpSaveRestart.Text = "Save && restart PHP";
        btnPhpSaveRestart.UseVisualStyleBackColor = true;
        btnPhpSaveRestart.Click += btnPhpSaveRestart_Click;
        //
        // PhpConfigForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnPhpCancel;
        ClientSize = new Size(764, 541);
        Controls.Add(tabPhpConfig);
        Controls.Add(panPhpFooter);
        Controls.Add(panPhpHeader);
        MinimumSize = new Size(700, 520);
        Name = "PhpConfigForm";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "PHP Configuration";

        panPhpHeader.ResumeLayout(false);
        tabPhpConfig.ResumeLayout(false);
        tabSettings.ResumeLayout(false);
        tblSettings.ResumeLayout(false);
        grpLimits.ResumeLayout(false);
        tblLimits.ResumeLayout(false);
        tblLimits.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudMemoryLimit).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudUploadMax).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudPostMax).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxExecTime).EndInit();
        grpErrors.ResumeLayout(false);
        tblErrors.ResumeLayout(false);
        tblErrors.PerformLayout();
        tabExtensions.ResumeLayout(false);
        tabExtensions.PerformLayout();
        tsExtensions.ResumeLayout(false);
        tsExtensions.PerformLayout();
        tabViewIni.ResumeLayout(false);
        tabViewIni.PerformLayout();
        tsIni.ResumeLayout(false);
        tsIni.PerformLayout();
        panPhpFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel panPhpHeader;
    private Label lblPhpTitle;
    private TabControl tabPhpConfig;
    private TabPage tabSettings;
    private TableLayoutPanel tblSettings;
    private GroupBox grpLimits;
    private TableLayoutPanel tblLimits;
    private Label lblMemory;
    private NumericUpDown nudMemoryLimit;
    private Label lblUpload;
    private NumericUpDown nudUploadMax;
    private Label lblPostMax;
    private NumericUpDown nudPostMax;
    private Label lblExecTime;
    private NumericUpDown nudMaxExecTime;
    private GroupBox grpErrors;
    private TableLayoutPanel tblErrors;
    private Label lblErrorRep;
    private ComboBox cmbErrorReporting;
    private Label lblDisplayErrors;
    private CheckBox chkDisplayErrors;
    private Label lblShortTag;
    private CheckBox chkShortOpenTag;
    private Label lblTimezone;
    private ComboBox cboTimezone;
    private TabPage tabExtensions;
    private ToolStrip tsExtensions;
    private ToolStripLabel tslExtFilter;
    private ToolStripTextBox txtExtFilter;
    private CheckedListBox clbExtensions;
    private TabPage tabViewIni;
    private ToolStrip tsIni;
    private ToolStripButton btnReloadIni;
    private ToolStripSeparator tsIniSep1;
    private ToolStripLabel lblIniHint;
    private RichTextBox rtbPhpIni;
    private Panel panPhpFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnPhpCancel;
    private Button btnPhpSave;
    private Button btnPhpSaveRestart;
}
