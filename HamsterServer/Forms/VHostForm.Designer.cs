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
        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, tsSep1, btnDelete });
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
        ctxVHosts.Items.AddRange(new ToolStripItem[] { ctxEdit, ctxDelete });
        ctxVHosts.Name = "ctxVHosts";
        ctxVHosts.Size = new Size(140, 48);
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
