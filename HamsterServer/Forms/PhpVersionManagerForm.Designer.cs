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
        toolSep2 = new ToolStripSeparator();
        btnDelete = new ToolStripButton();
        pnlContent = new Panel();
        lvVersions = new ListView();
        colVersion = new ColumnHeader();
        colStatus = new ColumnHeader();
        colPublished = new ColumnHeader();
        colPackage = new ColumnHeader();
        lblIntro = new Label();
        pnlProgress = new Panel();
        progressInstall = new ProgressBar();
        lblStatus = new Label();
        pnlFooter = new Panel();
        lblHint = new Label();
        btnCancel = new Button();
        btnClose = new Button();
        toolStrip.SuspendLayout();
        pnlContent.SuspendLayout();
        pnlProgress.SuspendLayout();
        pnlFooter.SuspendLayout();
        SuspendLayout();
        //
        // toolStrip
        //
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { btnRefresh, toolSep1, btnInstall, btnUpdate, toolSep2, btnDelete });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(784, 25);
        toolStrip.TabIndex = 0;
        //
        // btnRefresh
        //
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(66, 22);
        btnRefresh.Text = "Refresh";
        btnRefresh.ToolTipText = "Refresh the official PHP for Windows release catalog";
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
        btnInstall.ToolTipText = "Download, verify, and install the selected PHP release";
        btnInstall.Click += btnInstall_Click;
        //
        // btnUpdate
        //
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(65, 22);
        btnUpdate.Text = "Update";
        btnUpdate.ToolTipText = "Install the latest patch for this PHP branch side-by-side";
        btnUpdate.Click += btnUpdate_Click;
        //
        // toolSep2
        //
        toolSep2.Name = "toolSep2";
        toolSep2.Size = new Size(6, 25);
        //
        // btnDelete
        //
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(60, 22);
        btnDelete.Text = "Delete";
        btnDelete.ToolTipText = "Permanently delete the selected installed PHP runtime";
        btnDelete.Click += btnDelete_Click;
        //
        // pnlContent
        //
        pnlContent.Controls.Add(lvVersions);
        pnlContent.Controls.Add(lblIntro);
        pnlContent.Controls.Add(pnlProgress);
        pnlContent.Controls.Add(pnlFooter);
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Location = new Point(0, 25);
        pnlContent.Name = "pnlContent";
        pnlContent.Padding = new Padding(10, 8, 10, 0);
        pnlContent.Size = new Size(784, 436);
        pnlContent.TabIndex = 1;
        //
        // lvVersions
        //
        lvVersions.Columns.AddRange(new ColumnHeader[] { colVersion, colStatus, colPublished, colPackage });
        lvVersions.Dock = DockStyle.Fill;
        lvVersions.FullRowSelect = true;
        lvVersions.GridLines = true;
        lvVersions.HideSelection = false;
        lvVersions.Location = new Point(10, 48);
        lvVersions.MultiSelect = false;
        lvVersions.Name = "lvVersions";
        lvVersions.Size = new Size(764, 294);
        lvVersions.TabIndex = 1;
        lvVersions.UseCompatibleStateImageBehavior = false;
        lvVersions.View = View.Details;
        lvVersions.SelectedIndexChanged += lvVersions_SelectedIndexChanged;
        //
        // colVersion
        //
        colVersion.Text = "PHP version";
        colVersion.Width = 110;
        //
        // colStatus
        //
        colStatus.Text = "Status";
        colStatus.Width = 310;
        //
        // colPublished
        //
        colPublished.Text = "Published";
        colPublished.Width = 105;
        //
        // colPackage
        //
        colPackage.Text = "Package";
        colPackage.Width = 190;
        //
        // lblIntro
        //
        lblIntro.Dock = DockStyle.Top;
        lblIntro.Location = new Point(10, 8);
        lblIntro.Name = "lblIntro";
        lblIntro.Size = new Size(764, 40);
        lblIntro.TabIndex = 0;
        lblIntro.Text = "Install official PHP for Windows releases without copying folders manually. Updates are installed side-by-side to preserve rollback.";
        lblIntro.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pnlProgress
        //
        pnlProgress.Controls.Add(progressInstall);
        pnlProgress.Controls.Add(lblStatus);
        pnlProgress.Dock = DockStyle.Bottom;
        pnlProgress.Location = new Point(10, 342);
        pnlProgress.Name = "pnlProgress";
        pnlProgress.Padding = new Padding(0, 6, 0, 4);
        pnlProgress.Size = new Size(764, 51);
        pnlProgress.TabIndex = 2;
        //
        // progressInstall
        //
        progressInstall.Dock = DockStyle.Bottom;
        progressInstall.Location = new Point(0, 29);
        progressInstall.MarqueeAnimationSpeed = 25;
        progressInstall.Name = "progressInstall";
        progressInstall.Size = new Size(764, 18);
        progressInstall.TabIndex = 1;
        progressInstall.Visible = false;
        //
        // lblStatus
        //
        lblStatus.Dock = DockStyle.Top;
        lblStatus.Location = new Point(0, 6);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(764, 20);
        lblStatus.TabIndex = 0;
        lblStatus.Text = "Installed versions are available immediately; refresh to check php.net.";
        //
        // pnlFooter
        //
        pnlFooter.Controls.Add(lblHint);
        pnlFooter.Controls.Add(btnCancel);
        pnlFooter.Controls.Add(btnClose);
        pnlFooter.Dock = DockStyle.Bottom;
        pnlFooter.Location = new Point(10, 393);
        pnlFooter.Name = "pnlFooter";
        pnlFooter.Size = new Size(764, 43);
        pnlFooter.TabIndex = 3;
        //
        // lblHint
        //
        lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblHint.AutoEllipsis = true;
        lblHint.Location = new Point(0, 8);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(552, 30);
        lblHint.TabIndex = 0;
        lblHint.Text = "Official packages are Windows x64 Non Thread Safe builds from php.net.";
        //
        // btnCancel
        //
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.Location = new Point(574, 7);
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
        btnClose.Location = new Point(670, 7);
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
        ClientSize = new Size(784, 461);
        Controls.Add(pnlContent);
        Controls.Add(toolStrip);
        MinimumSize = new Size(700, 430);
        Name = "PhpVersionManagerForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "PHP Version Manager";
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        pnlContent.ResumeLayout(false);
        pnlProgress.ResumeLayout(false);
        pnlFooter.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip toolStrip;
    private ToolStripButton btnRefresh;
    private ToolStripSeparator toolSep1;
    private ToolStripButton btnInstall;
    private ToolStripButton btnUpdate;
    private ToolStripSeparator toolSep2;
    private ToolStripButton btnDelete;
    private Panel pnlContent;
    private Label lblIntro;
    private ListView lvVersions;
    private ColumnHeader colVersion;
    private ColumnHeader colStatus;
    private ColumnHeader colPublished;
    private ColumnHeader colPackage;
    private Panel pnlProgress;
    private Label lblStatus;
    private ProgressBar progressInstall;
    private Panel pnlFooter;
    private Label lblHint;
    private Button btnCancel;
    private Button btnClose;
}
