namespace HamsterServer.Forms;

partial class AboutForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // The logo bitmap is built from the application icon in the constructor, so unlike an
            // ImageList entry nothing else holds a reference to it.
            picLogo.Image?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        ttRoot = new ToolTip(components);
        panHeader = new Panel();
        panTitles = new Panel();
        lblSub = new Label();
        lblPublisher = new Label();
        lblVersion = new Label();
        lblTitle = new Label();
        picLogo = new PictureBox();
        grpInfo = new GroupBox();
        tblInfo = new TableLayoutPanel();
        lblApacheCaption = new Label();
        lblApache = new Label();
        lblPhpCaption = new Label();
        lblPhp = new Label();
        lblPhpInstalledCaption = new Label();
        lblPhpInstalled = new Label();
        lblRuntimeCaption = new Label();
        lblRuntime = new Label();
        lblOsCaption = new Label();
        lblOs = new Label();
        lblRootCaption = new Label();
        txtRoot = new TextBox();
        panFooter = new Panel();
        flpLinks = new FlowLayoutPanel();
        lnkRepository = new LinkLabel();
        lnkLicense = new LinkLabel();
        lnkSponsor = new LinkLabel();
        lnkPublisher = new LinkLabel();
        flpFooter = new FlowLayoutPanel();
        btnClose = new Button();
        btnCopyInfo = new Button();

        panHeader.SuspendLayout();
        panTitles.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        grpInfo.SuspendLayout();
        tblInfo.SuspendLayout();
        panFooter.SuspendLayout();
        flpLinks.SuspendLayout();
        flpFooter.SuspendLayout();
        SuspendLayout();

        //
        // panHeader
        //
        panHeader.Controls.Add(panTitles);
        panHeader.Controls.Add(picLogo);
        panHeader.Dock = DockStyle.Top;
        panHeader.Location = new Point(10, 10);
        panHeader.Name = "panHeader";
        panHeader.Padding = new Padding(0, 0, 0, 10);
        panHeader.Size = new Size(504, 126);
        panHeader.TabIndex = 0;
        //
        // picLogo
        //
        picLogo.Dock = DockStyle.Left;
        picLogo.Location = new Point(0, 0);
        picLogo.Name = "picLogo";
        picLogo.Size = new Size(72, 116);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 0;
        picLogo.TabStop = false;
        //
        // panTitles
        //
        panTitles.Controls.Add(lblPublisher);
        panTitles.Controls.Add(lblSub);
        panTitles.Controls.Add(lblVersion);
        panTitles.Controls.Add(lblTitle);
        panTitles.Dock = DockStyle.Fill;
        panTitles.Location = new Point(72, 0);
        panTitles.Name = "panTitles";
        panTitles.Padding = new Padding(14, 2, 0, 0);
        panTitles.Size = new Size(432, 116);
        panTitles.TabIndex = 1;
        //
        // lblTitle
        //
        lblTitle.AutoSize = true;
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font(Font.FontFamily, 15F, FontStyle.Bold);
        lblTitle.Location = new Point(14, 2);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(418, 28);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Hamster Server";
        //
        // lblVersion
        //
        lblVersion.AutoSize = true;
        lblVersion.Dock = DockStyle.Top;
        lblVersion.Location = new Point(14, 30);
        lblVersion.Name = "lblVersion";
        lblVersion.Padding = new Padding(0, 2, 0, 0);
        lblVersion.Size = new Size(418, 20);
        lblVersion.TabIndex = 1;
        lblVersion.Text = "Version";
        //
        // lblSub
        //
        lblSub.AutoSize = true;
        lblSub.Dock = DockStyle.Top;
        lblSub.ForeColor = SystemColors.GrayText;
        lblSub.Location = new Point(14, 50);
        lblSub.Name = "lblSub";
        lblSub.Padding = new Padding(0, 4, 0, 0);
        lblSub.Size = new Size(418, 22);
        lblSub.TabIndex = 2;
        lblSub.Text = "Local Apache + PHP development server for Windows.";
        //
        // lblPublisher
        //
        lblPublisher.AutoSize = true;
        lblPublisher.Dock = DockStyle.Top;
        lblPublisher.ForeColor = SystemColors.GrayText;
        lblPublisher.Location = new Point(14, 72);
        lblPublisher.Name = "lblPublisher";
        lblPublisher.Padding = new Padding(0, 4, 0, 0);
        lblPublisher.Size = new Size(418, 22);
        lblPublisher.TabIndex = 3;
        lblPublisher.Text = "A Konex Forge product.";
        //
        // grpInfo
        //
        grpInfo.Controls.Add(tblInfo);
        grpInfo.Dock = DockStyle.Fill;
        grpInfo.Location = new Point(10, 98);
        grpInfo.Name = "grpInfo";
        grpInfo.Padding = new Padding(10);
        grpInfo.Size = new Size(504, 194);
        grpInfo.TabIndex = 1;
        grpInfo.TabStop = false;
        grpInfo.Text = "Environment";
        //
        // tblInfo
        //
        tblInfo.ColumnCount = 2;
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblInfo.Controls.Add(lblApacheCaption, 0, 0);
        tblInfo.Controls.Add(lblApache, 1, 0);
        tblInfo.Controls.Add(lblPhpCaption, 0, 1);
        tblInfo.Controls.Add(lblPhp, 1, 1);
        tblInfo.Controls.Add(lblPhpInstalledCaption, 0, 2);
        tblInfo.Controls.Add(lblPhpInstalled, 1, 2);
        tblInfo.Controls.Add(lblRuntimeCaption, 0, 3);
        tblInfo.Controls.Add(lblRuntime, 1, 3);
        tblInfo.Controls.Add(lblOsCaption, 0, 4);
        tblInfo.Controls.Add(lblOs, 1, 4);
        tblInfo.Controls.Add(lblRootCaption, 0, 5);
        tblInfo.Controls.Add(txtRoot, 1, 5);
        tblInfo.Dock = DockStyle.Fill;
        tblInfo.Location = new Point(10, 26);
        tblInfo.Name = "tblInfo";
        tblInfo.RowCount = 6;
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblInfo.Size = new Size(484, 158);
        tblInfo.TabIndex = 0;
        //
        // lblApacheCaption
        //
        lblApacheCaption.Anchor = AnchorStyles.Left;
        lblApacheCaption.AutoSize = true;
        lblApacheCaption.Location = new Point(3, 5);
        lblApacheCaption.Name = "lblApacheCaption";
        lblApacheCaption.Size = new Size(48, 15);
        lblApacheCaption.TabIndex = 0;
        lblApacheCaption.Text = "Apache:";
        //
        // lblApache
        //
        lblApache.Anchor = AnchorStyles.Left;
        lblApache.AutoEllipsis = true;
        lblApache.Location = new Point(133, 5);
        lblApache.Name = "lblApache";
        lblApache.Size = new Size(348, 15);
        lblApache.TabIndex = 1;
        //
        // lblPhpCaption
        //
        lblPhpCaption.Anchor = AnchorStyles.Left;
        lblPhpCaption.AutoSize = true;
        lblPhpCaption.Location = new Point(3, 31);
        lblPhpCaption.Name = "lblPhpCaption";
        lblPhpCaption.Size = new Size(70, 15);
        lblPhpCaption.TabIndex = 2;
        lblPhpCaption.Text = "PHP (active):";
        //
        // lblPhp
        //
        lblPhp.Anchor = AnchorStyles.Left;
        lblPhp.AutoEllipsis = true;
        lblPhp.Location = new Point(133, 31);
        lblPhp.Name = "lblPhp";
        lblPhp.Size = new Size(348, 15);
        lblPhp.TabIndex = 3;
        //
        // lblPhpInstalledCaption
        //
        lblPhpInstalledCaption.Anchor = AnchorStyles.Left;
        lblPhpInstalledCaption.AutoSize = true;
        lblPhpInstalledCaption.Location = new Point(3, 57);
        lblPhpInstalledCaption.Name = "lblPhpInstalledCaption";
        lblPhpInstalledCaption.Size = new Size(80, 15);
        lblPhpInstalledCaption.TabIndex = 4;
        lblPhpInstalledCaption.Text = "PHP installed:";
        //
        // lblPhpInstalled
        //
        lblPhpInstalled.Anchor = AnchorStyles.Left;
        lblPhpInstalled.AutoEllipsis = true;
        lblPhpInstalled.Location = new Point(133, 57);
        lblPhpInstalled.Name = "lblPhpInstalled";
        lblPhpInstalled.Size = new Size(348, 15);
        lblPhpInstalled.TabIndex = 5;
        //
        // lblRuntimeCaption
        //
        lblRuntimeCaption.Anchor = AnchorStyles.Left;
        lblRuntimeCaption.AutoSize = true;
        lblRuntimeCaption.Location = new Point(3, 83);
        lblRuntimeCaption.Name = "lblRuntimeCaption";
        lblRuntimeCaption.Size = new Size(52, 15);
        lblRuntimeCaption.TabIndex = 6;
        lblRuntimeCaption.Text = "Runtime:";
        //
        // lblRuntime
        //
        lblRuntime.Anchor = AnchorStyles.Left;
        lblRuntime.AutoEllipsis = true;
        lblRuntime.Location = new Point(133, 83);
        lblRuntime.Name = "lblRuntime";
        lblRuntime.Size = new Size(348, 15);
        lblRuntime.TabIndex = 7;
        //
        // lblOsCaption
        //
        lblOsCaption.Anchor = AnchorStyles.Left;
        lblOsCaption.AutoSize = true;
        lblOsCaption.Location = new Point(3, 109);
        lblOsCaption.Name = "lblOsCaption";
        lblOsCaption.Size = new Size(58, 15);
        lblOsCaption.TabIndex = 8;
        lblOsCaption.Text = "Windows:";
        //
        // lblOs
        //
        lblOs.Anchor = AnchorStyles.Left;
        lblOs.AutoEllipsis = true;
        lblOs.Location = new Point(133, 109);
        lblOs.Name = "lblOs";
        lblOs.Size = new Size(348, 15);
        lblOs.TabIndex = 9;
        //
        // lblRootCaption
        //
        lblRootCaption.Anchor = AnchorStyles.Left;
        lblRootCaption.AutoSize = true;
        lblRootCaption.Location = new Point(3, 135);
        lblRootCaption.Name = "lblRootCaption";
        lblRootCaption.Size = new Size(88, 15);
        lblRootCaption.TabIndex = 10;
        lblRootCaption.Text = "Root directory:";
        //
        // txtRoot
        //
        // A read-only borderless TextBox rather than a Label: the path is long, often needs to be
        // pasted into a shell, and a Label cannot be selected.
        txtRoot.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtRoot.BackColor = SystemColors.Control;
        txtRoot.BorderStyle = BorderStyle.None;
        txtRoot.Location = new Point(133, 133);
        txtRoot.Name = "txtRoot";
        txtRoot.ReadOnly = true;
        txtRoot.Size = new Size(348, 16);
        txtRoot.TabIndex = 11;
        // Without this the first focusable control on the form is this box, so opening About
        // selected the whole path and scrolled it to the end — it read like a text field to fill in.
        txtRoot.TabStop = false;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Controls.Add(flpLinks);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 292);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(504, 76);
        panFooter.TabIndex = 2;
        //
        // flpLinks
        //
        flpLinks.Controls.Add(lnkRepository);
        flpLinks.Controls.Add(lnkLicense);
        flpLinks.Controls.Add(lnkSponsor);
        flpLinks.Controls.Add(lnkPublisher);
        flpLinks.Dock = DockStyle.Top;
        flpLinks.Location = new Point(0, 8);
        flpLinks.Name = "flpLinks";
        flpLinks.Size = new Size(504, 26);
        flpLinks.TabIndex = 0;
        flpLinks.WrapContents = false;
        //
        // lnkRepository
        //
        lnkRepository.AutoSize = true;
        lnkRepository.Location = new Point(3, 0);
        lnkRepository.Name = "lnkRepository";
        lnkRepository.Size = new Size(104, 15);
        lnkRepository.TabIndex = 0;
        lnkRepository.TabStop = true;
        lnkRepository.Text = "Project on GitHub";
        lnkRepository.LinkClicked += lnkRepository_LinkClicked;
        //
        // lnkLicense
        //
        lnkLicense.AutoSize = true;
        lnkLicense.Location = new Point(113, 0);
        lnkLicense.Name = "lnkLicense";
        lnkLicense.Size = new Size(72, 15);
        lnkLicense.TabIndex = 1;
        lnkLicense.TabStop = true;
        lnkLicense.Text = "MIT License";
        lnkLicense.LinkClicked += lnkLicense_LinkClicked;
        //
        // lnkSponsor
        //
        lnkSponsor.AutoSize = true;
        lnkSponsor.Location = new Point(191, 0);
        lnkSponsor.Name = "lnkSponsor";
        lnkSponsor.Size = new Size(56, 15);
        lnkSponsor.TabIndex = 2;
        lnkSponsor.TabStop = true;
        lnkSponsor.Text = "Sponsor";
        lnkSponsor.LinkClicked += lnkSponsor_LinkClicked;
        //
        // lnkPublisher
        //
        lnkPublisher.AutoSize = true;
        lnkPublisher.Location = new Point(253, 0);
        lnkPublisher.Name = "lnkPublisher";
        lnkPublisher.Size = new Size(82, 15);
        lnkPublisher.TabIndex = 3;
        lnkPublisher.TabStop = true;
        lnkPublisher.Text = "Konex Forge";
        lnkPublisher.LinkClicked += lnkPublisher_LinkClicked;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnClose);
        flpFooter.Controls.Add(btnCopyInfo);
        flpFooter.Dock = DockStyle.Bottom;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(0, 41);
        flpFooter.Name = "flpFooter";
        flpFooter.Size = new Size(504, 35);
        flpFooter.TabIndex = 1;
        flpFooter.WrapContents = false;
        //
        // btnClose
        //
        // AutoSize with a minimum: the captions change at runtime ("Copied", "Clipboard busy") and
        // a fixed width would clip them — as it would under a larger system font.
        btnClose.AutoSize = true;
        btnClose.AutoSizeMode = AutoSizeMode.GrowOnly;
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Location = new Point(411, 3);
        btnClose.MinimumSize = new Size(90, 27);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(90, 27);
        btnClose.TabIndex = 0;
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;
        //
        // btnCopyInfo
        //
        btnCopyInfo.AutoSize = true;
        btnCopyInfo.AutoSizeMode = AutoSizeMode.GrowOnly;
        btnCopyInfo.Location = new Point(255, 3);
        btnCopyInfo.MinimumSize = new Size(150, 27);
        btnCopyInfo.Name = "btnCopyInfo";
        btnCopyInfo.Size = new Size(150, 27);
        btnCopyInfo.TabIndex = 1;
        btnCopyInfo.Text = "Copy system info";
        btnCopyInfo.UseVisualStyleBackColor = true;
        btnCopyInfo.Click += btnCopyInfo_Click;
        //
        // AboutForm
        //
        AcceptButton = btnClose;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(524, 416);
        Controls.Add(grpInfo);
        Controls.Add(panFooter);
        Controls.Add(panHeader);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AboutForm";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "About Hamster Server";

        panHeader.ResumeLayout(false);
        panTitles.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        grpInfo.ResumeLayout(false);
        tblInfo.ResumeLayout(false);
        tblInfo.PerformLayout();
        panFooter.ResumeLayout(false);
        flpLinks.ResumeLayout(false);
        flpLinks.PerformLayout();
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private ToolTip ttRoot;
    private Panel panHeader;
    private PictureBox picLogo;
    private Panel panTitles;
    private Label lblTitle;
    private Label lblVersion;
    private Label lblSub;
    private Label lblPublisher;
    private GroupBox grpInfo;
    private TableLayoutPanel tblInfo;
    private Label lblApacheCaption;
    private Label lblApache;
    private Label lblPhpCaption;
    private Label lblPhp;
    private Label lblPhpInstalledCaption;
    private Label lblPhpInstalled;
    private Label lblRuntimeCaption;
    private Label lblRuntime;
    private Label lblOsCaption;
    private Label lblOs;
    private Label lblRootCaption;
    private TextBox txtRoot;
    private Panel panFooter;
    private FlowLayoutPanel flpLinks;
    private LinkLabel lnkRepository;
    private LinkLabel lnkLicense;
    private LinkLabel lnkSponsor;
    private LinkLabel lnkPublisher;
    private FlowLayoutPanel flpFooter;
    private Button btnClose;
    private Button btnCopyInfo;
}
