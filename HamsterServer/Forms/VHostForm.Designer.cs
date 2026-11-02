namespace HamsterServer.Forms;

partial class VHostForm
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

        toolStrip = new ToolStrip();
        btnAdd = new ToolStripButton();
        btnEdit = new ToolStripButton();
        tsSep1 = new ToolStripSeparator();
        btnDelete = new ToolStripButton();
        tsSep2 = new ToolStripSeparator();
        btnToggle = new ToolStripButton();
        tsSep3 = new ToolStripSeparator();
        btnBrowse = new ToolStripButton();
        btnOpenFolder = new ToolStripButton();
        lblIntro = new Label();
        lblSummary = new Label();
        ctxBrowse = new ToolStripMenuItem();
        ctxOpenFolder = new ToolStripMenuItem();
        ctxSep1 = new ToolStripSeparator();
        ctxToggle = new ToolStripMenuItem();
        lvVHosts = new ListView();
        colServerName = new ColumnHeader();
        colAlias = new ColumnHeader();
        colPort = new ColumnHeader();
        colFramework = new ColumnHeader();
        colDocRoot = new ColumnHeader();
        colEnabled = new ColumnHeader();
        ctxVHosts = new ContextMenuStrip(components);
        ctxEdit = new ToolStripMenuItem();
        ctxDelete = new ToolStripMenuItem();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnClose = new Button();

        toolStrip.SuspendLayout();
        ctxVHosts.SuspendLayout();
        panFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        SuspendLayout();

        //
        // toolStrip
        //
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, tsSep1, btnDelete, tsSep2, btnToggle, tsSep3, btnBrowse, btnOpenFolder });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(784, 25);
        toolStrip.TabIndex = 0;
        //
        // btnAdd
        //
        btnAdd.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(45, 22);
        btnAdd.Text = "Add…";
        btnAdd.ToolTipText = "Add a new virtual host";
        btnAdd.Click += btnAdd_Click;
        //
        // btnEdit
        //
        btnEdit.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(45, 22);
        btnEdit.Text = "Edit…";
        btnEdit.ToolTipText = "Edit the selected virtual host";
        btnEdit.Click += btnEdit_Click;
        //
        // tsSep1
        //
        tsSep1.Name = "tsSep1";
        tsSep1.Size = new Size(6, 25);
        //
        // btnDelete
        //
        btnDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(48, 22);
        btnDelete.Text = "Delete";
        btnDelete.ToolTipText = "Delete the selected virtual host";
        btnDelete.Click += btnDelete_Click;
        //
        // tsSep2
        //
        tsSep2.Name = "tsSep2";
        tsSep2.Size = new Size(6, 25);
        //
        // btnToggle
        //
        btnToggle.Name = "btnToggle";
        btnToggle.Size = new Size(70, 22);
        btnToggle.Text = "Disable";
        btnToggle.ToolTipText = "Switch the selected virtual host on or off without deleting it";
        btnToggle.Click += btnToggle_Click;
        //
        // tsSep3
        //
        tsSep3.Name = "tsSep3";
        tsSep3.Size = new Size(6, 25);
        //
        // btnBrowse
        //
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(110, 22);
        btnBrowse.Text = "Open in browser";
        btnBrowse.ToolTipText = "Open the selected site in the default browser";
        btnBrowse.Click += btnBrowse_Click;
        //
        // btnOpenFolder
        //
        btnOpenFolder.Name = "btnOpenFolder";
        btnOpenFolder.Size = new Size(110, 22);
        btnOpenFolder.Text = "Open folder";
        btnOpenFolder.ToolTipText = "Open the selected site's project folder in Explorer";
        btnOpenFolder.Click += btnOpenFolder_Click;
        //
        // lblIntro
        //
        // What the user needs to know before the first site works: the name must point at this
        // computer. Kept to one line so the list keeps its height.
        //
        lblIntro.Dock = DockStyle.Top;
        lblIntro.ForeColor = SystemColors.GrayText;
        lblIntro.Location = new Point(10, 25);
        lblIntro.Name = "lblIntro";
        lblIntro.Padding = new Padding(0, 6, 0, 6);
        lblIntro.Size = new Size(764, 46);
        lblIntro.TabIndex = 3;
        lblIntro.Text = "Each virtual host serves one project under its own name. Names ending in .localhost work at once; " +
            "other names (shop.test) need a line in the Windows hosts file.";
        //
        // lblSummary
        //
        lblSummary.Dock = DockStyle.Fill;
        lblSummary.ForeColor = SystemColors.GrayText;
        lblSummary.Location = new Point(0, 8);
        lblSummary.Name = "lblSummary";
        lblSummary.Size = new Size(664, 35);
        lblSummary.TabIndex = 1;
        lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        //
        // ctxBrowse
        //
        ctxBrowse.Name = "ctxBrowse";
        ctxBrowse.Size = new Size(180, 22);
        ctxBrowse.Text = "Open in browser";
        ctxBrowse.Click += btnBrowse_Click;
        //
        // ctxOpenFolder
        //
        ctxOpenFolder.Name = "ctxOpenFolder";
        ctxOpenFolder.Size = new Size(180, 22);
        ctxOpenFolder.Text = "Open folder";
        ctxOpenFolder.Click += btnOpenFolder_Click;
        //
        // ctxSep1
        //
        ctxSep1.Name = "ctxSep1";
        ctxSep1.Size = new Size(177, 6);
        //
        // ctxToggle
        //
        ctxToggle.Name = "ctxToggle";
        ctxToggle.Size = new Size(180, 22);
        ctxToggle.Text = "Disable";
        ctxToggle.Click += btnToggle_Click;
        //
        // lvVHosts
        //
        lvVHosts.Columns.AddRange(new ColumnHeader[] {
            colServerName, colAlias, colPort, colFramework, colDocRoot, colEnabled });
        lvVHosts.ContextMenuStrip = ctxVHosts;
        lvVHosts.Dock = DockStyle.Fill;
        lvVHosts.FullRowSelect = true;
        lvVHosts.GridLines = true;
        lvVHosts.HideSelection = false;
        lvVHosts.Location = new Point(10, 35);
        lvVHosts.MultiSelect = false;
        lvVHosts.Name = "lvVHosts";
        lvVHosts.OwnerDraw = true;
        lvVHosts.Size = new Size(764, 421);
        lvVHosts.TabIndex = 1;
        lvVHosts.UseCompatibleStateImageBehavior = false;
        lvVHosts.View = View.Details;
        lvVHosts.DrawColumnHeader += lvVHosts_DrawColumnHeader;
        lvVHosts.DrawItem += lvVHosts_DrawItem;
        lvVHosts.DrawSubItem += lvVHosts_DrawSubItem;
        lvVHosts.SelectedIndexChanged += lvVHosts_SelectedIndexChanged;
        lvVHosts.DoubleClick += lvVHosts_DoubleClick;
        lvVHosts.KeyDown += lvVHosts_KeyDown;
        lvVHosts.Resize += lvVHosts_Resize;
        //
        // colServerName
        //
        colServerName.Text = "Server name";
        colServerName.Width = 170;
        //
        // colAlias
        //
        colAlias.Text = "Aliases";
        colAlias.Width = 130;
        //
        // colPort
        //
        colPort.Text = "Port";
        colPort.TextAlign = HorizontalAlignment.Right;
        colPort.Width = 60;
        //
        // colFramework
        //
        colFramework.Text = "Framework";
        colFramework.Width = 150;
        //
        // colDocRoot
        //
        colDocRoot.Text = "Document root";
        colDocRoot.Width = 240;
        //
        // colEnabled
        //
        colEnabled.Text = "Status";
        colEnabled.Width = 80;
        //
        // ctxVHosts
        //
        ctxVHosts.Items.AddRange(new ToolStripItem[] { ctxBrowse, ctxOpenFolder, ctxSep1, ctxToggle, ctxEdit, ctxDelete });
        ctxVHosts.Name = "ctxVHosts";
        ctxVHosts.Size = new Size(181, 120);
        //
        // ctxEdit
        //
        ctxEdit.Name = "ctxEdit";
        ctxEdit.Size = new Size(139, 22);
        ctxEdit.Text = "Edit…";
        ctxEdit.Click += btnEdit_Click;
        //
        // ctxDelete
        //
        ctxDelete.Name = "ctxDelete";
        ctxDelete.Size = new Size(139, 22);
        ctxDelete.Text = "Delete";
        ctxDelete.Click += btnDelete_Click;
        //
        // panFooter
        //
        panFooter.Controls.Add(lblSummary);
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 456);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(764, 43);
        panFooter.TabIndex = 2;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnClose);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(664, 8);
        flpFooter.Name = "flpFooter";
        flpFooter.Size = new Size(100, 35);
        flpFooter.TabIndex = 0;
        flpFooter.WrapContents = false;
        //
        // btnClose
        //
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Location = new Point(7, 3);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(90, 27);
        btnClose.TabIndex = 0;
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;
        //
        // VHostForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(784, 509);
        Controls.Add(lvVHosts);
        Controls.Add(lblIntro);
        Controls.Add(panFooter);
        Controls.Add(toolStrip);
        MinimumSize = new Size(680, 400);
        Name = "VHostForm";
        Padding = new Padding(10, 0, 10, 10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Virtual Hosts";

        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        ctxVHosts.ResumeLayout(false);
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip toolStrip;
    private ToolStripButton btnAdd;
    private ToolStripButton btnEdit;
    private ToolStripSeparator tsSep1;
    private ToolStripButton btnDelete;
    private ToolStripSeparator tsSep2;
    private ToolStripButton btnToggle;
    private ToolStripSeparator tsSep3;
    private ToolStripButton btnBrowse;
    private ToolStripButton btnOpenFolder;
    private Label lblIntro;
    private Label lblSummary;
    private ToolStripMenuItem ctxBrowse;
    private ToolStripMenuItem ctxOpenFolder;
    private ToolStripSeparator ctxSep1;
    private ToolStripMenuItem ctxToggle;
    private ListView lvVHosts;
    private ColumnHeader colServerName;
    private ColumnHeader colAlias;
    private ColumnHeader colPort;
    private ColumnHeader colFramework;
    private ColumnHeader colDocRoot;
    private ColumnHeader colEnabled;
    private ContextMenuStrip ctxVHosts;
    private ToolStripMenuItem ctxEdit;
    private ToolStripMenuItem ctxDelete;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnClose;
}
