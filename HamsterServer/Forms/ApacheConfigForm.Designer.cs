namespace HamsterServer.Forms;

partial class ApacheConfigForm
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

        tabApacheConfig = new TabControl();
        tabViewConf = new TabPage();
        rtbHttpdConf = new RichTextBox();
        tsViewConf = new ToolStrip();
        btnReloadConf = new ToolStripButton();
        tsViewSep1 = new ToolStripSeparator();
        lblViewHint = new ToolStripLabel();
        tabCustomDirectives = new TabPage();
        txtCustomDirectives = new TextBox();
        lblCustomHint = new Label();
        tabVHostFiles = new TabPage();
        splitVHost = new SplitContainer();
        lstVHostFiles = new ListBox();
        rtbVHostContent = new RichTextBox();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnApacheCancel = new Button();
        btnApacheSave = new Button();

        tabApacheConfig.SuspendLayout();
        tabViewConf.SuspendLayout();
        tsViewConf.SuspendLayout();
        tabCustomDirectives.SuspendLayout();
        tabVHostFiles.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitVHost).BeginInit();
        splitVHost.Panel1.SuspendLayout();
        splitVHost.Panel2.SuspendLayout();
        splitVHost.SuspendLayout();
        panFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        SuspendLayout();

        //
        // tabApacheConfig
        //
        tabApacheConfig.Controls.Add(tabViewConf);
        tabApacheConfig.Controls.Add(tabCustomDirectives);
        tabApacheConfig.Controls.Add(tabVHostFiles);
        tabApacheConfig.Dock = DockStyle.Fill;
        tabApacheConfig.Location = new Point(10, 10);
        tabApacheConfig.Name = "tabApacheConfig";
        tabApacheConfig.SelectedIndex = 0;
        tabApacheConfig.Size = new Size(824, 508);
        tabApacheConfig.TabIndex = 0;
        //
        // tabViewConf
        //
        tabViewConf.Controls.Add(rtbHttpdConf);
        tabViewConf.Controls.Add(tsViewConf);
        tabViewConf.Location = new Point(4, 24);
        tabViewConf.Name = "tabViewConf";
        tabViewConf.Padding = new Padding(6);
        tabViewConf.Size = new Size(816, 480);
        tabViewConf.TabIndex = 0;
        tabViewConf.Text = "httpd.conf";
        tabViewConf.UseVisualStyleBackColor = true;
        //
        // rtbHttpdConf
        //
        rtbHttpdConf.BackColor = SystemColors.Window;
        rtbHttpdConf.BorderStyle = BorderStyle.Fixed3D;
        rtbHttpdConf.Dock = DockStyle.Fill;
        rtbHttpdConf.Location = new Point(6, 31);
        rtbHttpdConf.Name = "rtbHttpdConf";
        rtbHttpdConf.ReadOnly = true;
        rtbHttpdConf.Size = new Size(804, 443);
        rtbHttpdConf.TabIndex = 1;
        rtbHttpdConf.Text = "";
        rtbHttpdConf.WordWrap = false;
        //
        // tsViewConf
        //
        tsViewConf.Dock = DockStyle.Top;
        tsViewConf.GripStyle = ToolStripGripStyle.Hidden;
        tsViewConf.Items.AddRange(new ToolStripItem[] { btnReloadConf, tsViewSep1, lblViewHint });
        tsViewConf.Location = new Point(6, 6);
        tsViewConf.Name = "tsViewConf";
        tsViewConf.Size = new Size(804, 25);
        tsViewConf.TabIndex = 0;
        //
        // btnReloadConf
        //
        btnReloadConf.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnReloadConf.Name = "btnReloadConf";
        btnReloadConf.Size = new Size(52, 22);
        btnReloadConf.Text = "Reload";
        btnReloadConf.ToolTipText = "Re-read httpd.conf from disk";
        btnReloadConf.Click += btnReloadConf_Click;
        //
        // tsViewSep1
        //
        tsViewSep1.Name = "tsViewSep1";
        tsViewSep1.Size = new Size(6, 25);
        //
        // lblViewHint
        //
        lblViewHint.ForeColor = SystemColors.GrayText;
        lblViewHint.Name = "lblViewHint";
        lblViewHint.Size = new Size(400, 22);
        lblViewHint.Text = "Read-only — Hamster Server regenerates this file every time Apache starts.";
        //
        // tabCustomDirectives
        //
        tabCustomDirectives.Controls.Add(txtCustomDirectives);
        tabCustomDirectives.Controls.Add(lblCustomHint);
        tabCustomDirectives.Location = new Point(4, 24);
        tabCustomDirectives.Name = "tabCustomDirectives";
        tabCustomDirectives.Padding = new Padding(6);
        tabCustomDirectives.Size = new Size(816, 480);
        tabCustomDirectives.TabIndex = 1;
        tabCustomDirectives.Text = "Custom directives";
        tabCustomDirectives.UseVisualStyleBackColor = true;
        //
        // txtCustomDirectives
        //
        txtCustomDirectives.AcceptsReturn = true;
        txtCustomDirectives.AcceptsTab = true;
        txtCustomDirectives.Dock = DockStyle.Fill;
        txtCustomDirectives.Location = new Point(6, 44);
        txtCustomDirectives.Multiline = true;
        txtCustomDirectives.Name = "txtCustomDirectives";
        txtCustomDirectives.ScrollBars = ScrollBars.Both;
        txtCustomDirectives.Size = new Size(804, 430);
        txtCustomDirectives.TabIndex = 1;
        txtCustomDirectives.WordWrap = false;
        //
        // lblCustomHint
        //
        lblCustomHint.Dock = DockStyle.Top;
        lblCustomHint.ForeColor = SystemColors.GrayText;
        lblCustomHint.Location = new Point(6, 6);
        lblCustomHint.Name = "lblCustomHint";
        lblCustomHint.Padding = new Padding(0, 0, 0, 6);
        lblCustomHint.Size = new Size(804, 38);
        lblCustomHint.TabIndex = 0;
        lblCustomHint.Text = "These directives are appended to the generated httpd.conf. They survive regeneration — unlike edits made directly to httpd.conf.";
        //
        // tabVHostFiles
        //
        tabVHostFiles.Controls.Add(splitVHost);
        tabVHostFiles.Location = new Point(4, 24);
        tabVHostFiles.Name = "tabVHostFiles";
        tabVHostFiles.Padding = new Padding(6);
        tabVHostFiles.Size = new Size(816, 480);
        tabVHostFiles.TabIndex = 2;
        tabVHostFiles.Text = "VHost files";
        tabVHostFiles.UseVisualStyleBackColor = true;
        //
        // splitVHost
        //
        splitVHost.Dock = DockStyle.Fill;
        splitVHost.FixedPanel = FixedPanel.Panel1;
        splitVHost.Location = new Point(6, 6);
        splitVHost.Name = "splitVHost";
        splitVHost.Panel1.Controls.Add(lstVHostFiles);
        splitVHost.Panel1MinSize = 140;
        splitVHost.Panel2.Controls.Add(rtbVHostContent);
        splitVHost.Panel2MinSize = 220;
        splitVHost.Size = new Size(804, 468);
        splitVHost.SplitterDistance = 220;
        splitVHost.TabIndex = 0;
        //
        // lstVHostFiles
        //
        lstVHostFiles.Dock = DockStyle.Fill;
        lstVHostFiles.IntegralHeight = false;
        lstVHostFiles.ItemHeight = 15;
        lstVHostFiles.Location = new Point(0, 0);
        lstVHostFiles.Name = "lstVHostFiles";
        lstVHostFiles.Size = new Size(220, 468);
        lstVHostFiles.TabIndex = 0;
        lstVHostFiles.SelectedIndexChanged += lstVHostFiles_SelectedIndexChanged;
        //
        // rtbVHostContent
        //
        rtbVHostContent.BackColor = SystemColors.Window;
        rtbVHostContent.BorderStyle = BorderStyle.Fixed3D;
        rtbVHostContent.Dock = DockStyle.Fill;
        rtbVHostContent.Location = new Point(0, 0);
        rtbVHostContent.Name = "rtbVHostContent";
        rtbVHostContent.ReadOnly = true;
        rtbVHostContent.Size = new Size(580, 468);
        rtbVHostContent.TabIndex = 0;
        rtbVHostContent.Text = "";
        rtbVHostContent.WordWrap = false;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 518);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(824, 43);
        panFooter.TabIndex = 1;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnApacheCancel);
        flpFooter.Controls.Add(btnApacheSave);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(624, 8);
        flpFooter.Name = "flpFooter";
        flpFooter.Size = new Size(200, 35);
        flpFooter.TabIndex = 0;
        flpFooter.WrapContents = false;
        //
        // btnApacheCancel
        //
        btnApacheCancel.DialogResult = DialogResult.Cancel;
        btnApacheCancel.Location = new Point(107, 3);
        btnApacheCancel.Name = "btnApacheCancel";
        btnApacheCancel.Size = new Size(90, 27);
        btnApacheCancel.TabIndex = 1;
        btnApacheCancel.Text = "Cancel";
        btnApacheCancel.UseVisualStyleBackColor = true;
        btnApacheCancel.Click += btnApacheCancel_Click;
        //
        // btnApacheSave
        //
        btnApacheSave.Location = new Point(11, 3);
        btnApacheSave.Name = "btnApacheSave";
        btnApacheSave.Size = new Size(90, 27);
        btnApacheSave.TabIndex = 0;
        btnApacheSave.Text = "OK";
        btnApacheSave.UseVisualStyleBackColor = true;
        btnApacheSave.Click += btnApacheSave_Click;
        //
        // ApacheConfigForm
        //
        AcceptButton = btnApacheSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnApacheCancel;
        ClientSize = new Size(844, 571);
        Controls.Add(tabApacheConfig);
        Controls.Add(panFooter);
        MinimumSize = new Size(700, 480);
        Name = "ApacheConfigForm";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Apache Configuration";

        tabApacheConfig.ResumeLayout(false);
        tabViewConf.ResumeLayout(false);
        tabViewConf.PerformLayout();
        tsViewConf.ResumeLayout(false);
        tsViewConf.PerformLayout();
        tabCustomDirectives.ResumeLayout(false);
        tabCustomDirectives.PerformLayout();
        tabVHostFiles.ResumeLayout(false);
        splitVHost.Panel1.ResumeLayout(false);
        splitVHost.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitVHost).EndInit();
        splitVHost.ResumeLayout(false);
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TabControl tabApacheConfig;
    private TabPage tabViewConf;
    private ToolStrip tsViewConf;
    private ToolStripButton btnReloadConf;
    private ToolStripSeparator tsViewSep1;
    private ToolStripLabel lblViewHint;
    private RichTextBox rtbHttpdConf;
    private TabPage tabCustomDirectives;
    private Label lblCustomHint;
    private TextBox txtCustomDirectives;
    private TabPage tabVHostFiles;
    private SplitContainer splitVHost;
    private ListBox lstVHostFiles;
    private RichTextBox rtbVHostContent;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnApacheCancel;
    private Button btnApacheSave;
}
