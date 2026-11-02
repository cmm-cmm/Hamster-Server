namespace HamsterServer.Forms;

partial class PhpVersionManagerForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolStrip = new ToolStrip();
        btnRefresh = new ToolStripButton();
        toolSep1 = new ToolStripSeparator();
        btnInstall = new ToolStripButton();
        btnUpdate = new ToolStripButton();
        btnUse = new ToolStripButton();
        toolSep2 = new ToolStripSeparator();
        btnCopyExtensions = new ToolStripButton();
        toolSep3 = new ToolStripSeparator();
        btnDelete = new ToolStripButton();
        btnCleanup = new ToolStripButton();
        tblSummary = new TableLayoutPanel();
        lblActiveTitle = new Label();
        lblActiveDetail = new Label();
        btnUpdateActive = new Button();
        flpUpdateMode = new FlowLayoutPanel();
        lblUpdateMode = new Label();
        cmbUpdateMode = new ComboBox();
        pnlContent = new Panel();
        lvVersions = new ListView();
        colVersion = new ColumnHeader();
        colStatus = new ColumnHeader();
        colExtensions = new ColumnHeader();
        colPublished = new ColumnHeader();
        colSize = new ColumnHeader();
        grpDetails = new GroupBox();
        lblDetails = new Label();
        pnlProgress = new Panel();
        progressInstall = new ProgressBar();
        lblStatus = new Label();
        pnlFooter = new Panel();
        lblHint = new Label();
        btnCancel = new Button();
        btnClose = new Button();
        toolStrip.SuspendLayout();
        tblSummary.SuspendLayout();
        flpUpdateMode.SuspendLayout();
        pnlContent.SuspendLayout();
        grpDetails.SuspendLayout();
        pnlProgress.SuspendLayout();
        pnlFooter.SuspendLayout();
        SuspendLayout();
        //
        // toolStrip
        //
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] {
            btnRefresh, toolSep1, btnInstall, btnUpdate, btnUse, toolSep2, btnCopyExtensions, toolSep3, btnDelete, btnCleanup });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(884, 25);
        toolStrip.TabIndex = 0;
        //
        // btnRefresh
        //
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(120, 22);
        btnRefresh.Text = "Check for updates";
        btnRefresh.ToolTipText = "Reload the official PHP for Windows release list from windows.php.net";
        btnRefresh.Click += btnRefresh_Click;
        //
        // toolSep1
        //
        toolSep1.Name = "toolSep1";
        toolSep1.Size = new Size(6, 25);
        //
        // btnInstall
        //
        btnInstall.Name = "btnInstall";
        btnInstall.Size = new Size(60, 22);
        btnInstall.Text = "Install";
        btnInstall.ToolTipText = "Download, verify and install the selected release side-by-side";
        btnInstall.Click += btnInstall_Click;
        //
        // btnUpdate
        //
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(65, 22);
        btnUpdate.Text = "Update";
        btnUpdate.ToolTipText = "Install the latest patch of the selected runtime's branch and carry its extensions over";
        btnUpdate.Click += btnUpdate_Click;
        //
        // btnUse
        //
        btnUse.Name = "btnUse";
        btnUse.Size = new Size(110, 22);
        btnUse.Text = "Use this version";
        btnUse.ToolTipText = "Make the selected runtime the active one (restarts running services)";
        btnUse.Click += btnUse_Click;
        //
        // toolSep2
        //
        toolSep2.Name = "toolSep2";
        toolSep2.Size = new Size(6, 25);
        //
        // btnCopyExtensions
        //
        btnCopyExtensions.Name = "btnCopyExtensions";
        btnCopyExtensions.Size = new Size(110, 22);
        btnCopyExtensions.Text = "Copy extensions";
        btnCopyExtensions.ToolTipText = "Copy extension DLLs that another runtime of the same branch has and this one lacks";
        btnCopyExtensions.Click += btnCopyExtensions_Click;
        //
        // toolSep3
        //
        toolSep3.Name = "toolSep3";
        toolSep3.Size = new Size(6, 25);
        //
        // btnDelete
        //
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(66, 22);
        btnDelete.Text = "Remove";
        btnDelete.ToolTipText = "Permanently delete the selected installed runtime";
        btnDelete.Click += btnDelete_Click;
        //
        // btnCleanup
        //
        btnCleanup.Name = "btnCleanup";
        btnCleanup.Size = new Size(126, 22);
        btnCleanup.Text = "Remove old patches";
        btnCleanup.ToolTipText = "Delete older patches that a newer installed patch of the same branch replaces";
        btnCleanup.Click += btnCleanup_Click;
        //
        // tblSummary
        //
        // The answer to "which PHP am I on, and is it current?" — the question the old flat list made
        // the user work out by reading every row.
        //
        tblSummary.ColumnCount = 3;
        tblSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblSummary.ColumnStyles.Add(new ColumnStyle());
        tblSummary.ColumnStyles.Add(new ColumnStyle());
        tblSummary.Controls.Add(lblActiveTitle, 0, 0);
        tblSummary.Controls.Add(lblActiveDetail, 0, 1);
        tblSummary.Controls.Add(btnUpdateActive, 1, 0);
        tblSummary.Controls.Add(flpUpdateMode, 2, 0);
        tblSummary.SetRowSpan(btnUpdateActive, 2);
        tblSummary.SetRowSpan(flpUpdateMode, 2);
        tblSummary.Dock = DockStyle.Top;
        tblSummary.Location = new Point(0, 25);
        tblSummary.Name = "tblSummary";
        tblSummary.Padding = new Padding(10, 8, 10, 4);
        tblSummary.RowCount = 2;
        tblSummary.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblSummary.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblSummary.Size = new Size(884, 68);
        tblSummary.TabIndex = 1;
        //
        // lblActiveTitle
        //
        lblActiveTitle.AutoEllipsis = true;
        lblActiveTitle.Dock = DockStyle.Fill;
        lblActiveTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblActiveTitle.Location = new Point(13, 8);
        lblActiveTitle.Name = "lblActiveTitle";
        lblActiveTitle.Size = new Size(470, 30);
        lblActiveTitle.TabIndex = 0;
        lblActiveTitle.Text = "PHP";
        lblActiveTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblActiveDetail
        //
        lblActiveDetail.AutoEllipsis = true;
        lblActiveDetail.Dock = DockStyle.Fill;
        lblActiveDetail.Location = new Point(13, 38);
        lblActiveDetail.Name = "lblActiveDetail";
        lblActiveDetail.Size = new Size(470, 26);
        lblActiveDetail.TabIndex = 1;
        lblActiveDetail.Text = "Checking windows.php.net…";
        lblActiveDetail.TextAlign = ContentAlignment.TopLeft;
        //
        // btnUpdateActive
        //
        btnUpdateActive.Anchor = AnchorStyles.Right;
        btnUpdateActive.AutoSize = true;
        btnUpdateActive.Location = new Point(489, 19);
        btnUpdateActive.MinimumSize = new Size(150, 30);
        btnUpdateActive.Name = "btnUpdateActive";
        btnUpdateActive.Padding = new Padding(6, 0, 6, 0);
        btnUpdateActive.Size = new Size(150, 30);
        btnUpdateActive.TabIndex = 2;
        btnUpdateActive.Text = "Update now";
        btnUpdateActive.UseVisualStyleBackColor = true;
        btnUpdateActive.Visible = false;
        btnUpdateActive.Click += btnUpdateActive_Click;
        //
        // flpUpdateMode
        //
        flpUpdateMode.Anchor = AnchorStyles.Right;
        flpUpdateMode.AutoSize = true;
        flpUpdateMode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpUpdateMode.Controls.Add(lblUpdateMode);
        flpUpdateMode.Controls.Add(cmbUpdateMode);
        flpUpdateMode.FlowDirection = FlowDirection.TopDown;
        flpUpdateMode.Location = new Point(645, 9);
        flpUpdateMode.Margin = new Padding(12, 0, 0, 0);
        flpUpdateMode.Name = "flpUpdateMode";
        flpUpdateMode.Size = new Size(226, 50);
        flpUpdateMode.TabIndex = 3;
        flpUpdateMode.WrapContents = false;
        //
        // lblUpdateMode
        //
        lblUpdateMode.AutoSize = true;
        lblUpdateMode.Location = new Point(3, 0);
        lblUpdateMode.Name = "lblUpdateMode";
        lblUpdateMode.Size = new Size(144, 15);
        lblUpdateMode.TabIndex = 0;
        lblUpdateMode.Text = "Patch updates for this branch:";
        //
        // cmbUpdateMode
        //
        cmbUpdateMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbUpdateMode.FormattingEnabled = true;
        cmbUpdateMode.Items.AddRange(new object[] { "Install automatically", "Notify me only", "Off" });
        cmbUpdateMode.Location = new Point(3, 18);
        cmbUpdateMode.Name = "cmbUpdateMode";
        cmbUpdateMode.Size = new Size(220, 23);
        cmbUpdateMode.TabIndex = 1;
        cmbUpdateMode.SelectedIndexChanged += cmbUpdateMode_SelectedIndexChanged;
        //
        // pnlContent
        //
        pnlContent.Controls.Add(lvVersions);
        pnlContent.Controls.Add(grpDetails);
        pnlContent.Controls.Add(pnlProgress);
        pnlContent.Controls.Add(pnlFooter);
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Location = new Point(0, 93);
        pnlContent.Name = "pnlContent";
        pnlContent.Padding = new Padding(10, 4, 10, 0);
        pnlContent.Size = new Size(884, 508);
        pnlContent.TabIndex = 2;
        //
        // lvVersions
        //
        lvVersions.Columns.AddRange(new ColumnHeader[] { colVersion, colStatus, colExtensions, colPublished, colSize });
        lvVersions.Dock = DockStyle.Fill;
        lvVersions.FullRowSelect = true;
        lvVersions.HideSelection = false;
        lvVersions.Location = new Point(10, 4);
        lvVersions.MultiSelect = false;
        lvVersions.Name = "lvVersions";
        lvVersions.ShowGroups = true;
        lvVersions.Size = new Size(864, 270);
        lvVersions.TabIndex = 0;
        lvVersions.UseCompatibleStateImageBehavior = false;
        lvVersions.View = View.Details;
        lvVersions.SelectedIndexChanged += lvVersions_SelectedIndexChanged;
        lvVersions.DoubleClick += lvVersions_DoubleClick;
        lvVersions.Resize += lvVersions_Resize;
        //
        // colVersion
        //
        colVersion.Text = "Version";
        colVersion.Width = 110;
        //
        // colStatus
        //
        colStatus.Text = "Status";
        colStatus.Width = 230;
        //
        // colExtensions
        //
        colExtensions.Text = "Extensions";
        colExtensions.Width = 280;
        //
        // colPublished
        //
        colPublished.Text = "Released";
        colPublished.Width = 95;
        //
        // colSize
        //
        colSize.Text = "Download";
        colSize.Width = 90;
        //
        // grpDetails
        //
        grpDetails.Controls.Add(lblDetails);
        grpDetails.Dock = DockStyle.Bottom;
        grpDetails.Location = new Point(10, 274);
        grpDetails.Name = "grpDetails";
        grpDetails.Padding = new Padding(8, 4, 8, 6);
        grpDetails.Size = new Size(864, 100);
        grpDetails.TabIndex = 1;
        grpDetails.TabStop = false;
        grpDetails.Text = "Details";
        //
        // lblDetails
        //
        lblDetails.Dock = DockStyle.Fill;
        lblDetails.Location = new Point(8, 20);
        lblDetails.Name = "lblDetails";
        lblDetails.Size = new Size(848, 74);
        lblDetails.TabIndex = 0;
        lblDetails.Text = "Select a version.";
        //
        // pnlProgress
        //
        pnlProgress.Controls.Add(progressInstall);
        pnlProgress.Controls.Add(lblStatus);
        pnlProgress.Dock = DockStyle.Bottom;
        pnlProgress.Location = new Point(10, 404);
        pnlProgress.Name = "pnlProgress";
        pnlProgress.Padding = new Padding(0, 6, 0, 4);
        pnlProgress.Size = new Size(864, 51);
        pnlProgress.TabIndex = 2;
        //
        // progressInstall
        //
        progressInstall.Dock = DockStyle.Bottom;
        progressInstall.Location = new Point(0, 29);
        progressInstall.MarqueeAnimationSpeed = 25;
        progressInstall.Name = "progressInstall";
        progressInstall.Size = new Size(864, 18);
        progressInstall.TabIndex = 1;
        progressInstall.Visible = false;
        //
        // lblStatus
        //
        lblStatus.AutoEllipsis = true;
        lblStatus.Dock = DockStyle.Top;
        lblStatus.Location = new Point(0, 6);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(864, 20);
        lblStatus.TabIndex = 0;
        lblStatus.Text = "";
        //
        // pnlFooter
        //
        pnlFooter.Controls.Add(lblHint);
        pnlFooter.Controls.Add(btnCancel);
        pnlFooter.Controls.Add(btnClose);
        pnlFooter.Dock = DockStyle.Bottom;
        pnlFooter.Location = new Point(10, 455);
        pnlFooter.Name = "pnlFooter";
        pnlFooter.Size = new Size(864, 53);
        pnlFooter.TabIndex = 3;
        //
        // lblHint
        //
        lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblHint.AutoEllipsis = true;
        lblHint.ForeColor = SystemColors.GrayText;
        lblHint.Location = new Point(0, 8);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(652, 36);
        lblHint.TabIndex = 0;
        lblHint.Text = "Official Windows x64 Non Thread Safe builds from php.net, verified with SHA-256.";
        //
        // btnCancel
        //
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.Location = new Point(674, 10);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 27);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Visible = false;
        btnCancel.Click += btnCancel_Click;
        //
        // btnClose
        //
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Location = new Point(770, 10);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(90, 27);
        btnClose.TabIndex = 2;
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;
        //
        // PhpVersionManagerForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(884, 601);
        Controls.Add(pnlContent);
        Controls.Add(tblSummary);
        Controls.Add(toolStrip);
        MinimumSize = new Size(780, 560);
        Name = "PhpVersionManagerForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "PHP Version Manager";
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        flpUpdateMode.ResumeLayout(false);
        flpUpdateMode.PerformLayout();
        tblSummary.ResumeLayout(false);
        tblSummary.PerformLayout();
        grpDetails.ResumeLayout(false);
        pnlProgress.ResumeLayout(false);
        pnlFooter.ResumeLayout(false);
        pnlContent.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip toolStrip;
    private ToolStripButton btnRefresh;
    private ToolStripSeparator toolSep1;
    private ToolStripButton btnInstall;
    private ToolStripButton btnUpdate;
    private ToolStripButton btnUse;
    private ToolStripSeparator toolSep2;
    private ToolStripButton btnCopyExtensions;
    private ToolStripSeparator toolSep3;
    private ToolStripButton btnDelete;
    private ToolStripButton btnCleanup;
    private TableLayoutPanel tblSummary;
    private Label lblActiveTitle;
    private Label lblActiveDetail;
    private Button btnUpdateActive;
    private FlowLayoutPanel flpUpdateMode;
    private Label lblUpdateMode;
    private ComboBox cmbUpdateMode;
    private Panel pnlContent;
    private ListView lvVersions;
    private ColumnHeader colVersion;
    private ColumnHeader colStatus;
    private ColumnHeader colExtensions;
    private ColumnHeader colPublished;
    private ColumnHeader colSize;
    private GroupBox grpDetails;
    private Label lblDetails;
    private Panel pnlProgress;
    private Label lblStatus;
    private ProgressBar progressInstall;
    private Panel pnlFooter;
    private Label lblHint;
    private Button btnCancel;
    private Button btnClose;
}
