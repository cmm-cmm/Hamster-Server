namespace HamsterServer.Forms;

partial class PortsForm
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
        btnRemove = new ToolStripButton();
        lvPorts = new ListView();
        colPort = new ColumnHeader();
        colSsl = new ColumnHeader();
        colKind = new ColumnHeader();
        colStatus = new ColumnHeader();
        ctxPorts = new ContextMenuStrip(components);
        ctxEdit = new ToolStripMenuItem();
        ctxRemove = new ToolStripMenuItem();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnClose = new Button();
        statusStrip = new StatusStrip();
        lblHint = new ToolStripStatusLabel();

        toolStrip.SuspendLayout();
        ctxPorts.SuspendLayout();
        panFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        //
        // toolStrip
        //
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, tsSep1, btnRemove });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(544, 25);
        toolStrip.TabIndex = 0;
        //
        // btnAdd
        //
        btnAdd.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(45, 22);
        btnAdd.Text = "Add…";
        btnAdd.ToolTipText = "Add a new listening port";
        btnAdd.Click += btnAdd_Click;
        //
        // btnEdit
        //
        btnEdit.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(45, 22);
        btnEdit.Text = "Edit…";
        btnEdit.ToolTipText = "Edit the selected port";
        btnEdit.Click += btnEdit_Click;
        //
        // tsSep1
        //
        tsSep1.Name = "tsSep1";
        tsSep1.Size = new Size(6, 25);
        //
        // btnRemove
        //
        btnRemove.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnRemove.Name = "btnRemove";
        btnRemove.Size = new Size(60, 22);
        btnRemove.Text = "Remove";
        btnRemove.ToolTipText = "Remove the selected extra port (primary ports cannot be removed)";
        btnRemove.Click += btnRemove_Click;
        //
        // lvPorts
        //
        lvPorts.CheckBoxes = true;
        lvPorts.Columns.AddRange(new ColumnHeader[] { colPort, colSsl, colKind, colStatus });
        lvPorts.ContextMenuStrip = ctxPorts;
        lvPorts.Dock = DockStyle.Fill;
        lvPorts.FullRowSelect = true;
        lvPorts.GridLines = true;
        lvPorts.HideSelection = false;
        lvPorts.Location = new Point(10, 35);
        lvPorts.MultiSelect = false;
        lvPorts.Name = "lvPorts";
        lvPorts.Size = new Size(524, 279);
        lvPorts.TabIndex = 1;
        lvPorts.UseCompatibleStateImageBehavior = false;
        lvPorts.View = View.Details;
        lvPorts.SelectedIndexChanged += lvPorts_SelectedIndexChanged;
        lvPorts.DoubleClick += lvPorts_DoubleClick;
        lvPorts.KeyDown += lvPorts_KeyDown;
        lvPorts.Resize += lvPorts_Resize;
        //
        // colPort
        //
        colPort.Text = "Port";
        colPort.TextAlign = HorizontalAlignment.Right;
        colPort.Width = 80;
        //
        // colSsl
        //
        colSsl.Text = "SSL";
        colSsl.TextAlign = HorizontalAlignment.Center;
        colSsl.Width = 60;
        //
        // colKind
        //
        colKind.Text = "Kind";
        colKind.Width = 180;
        //
        // colStatus
        //
        colStatus.Text = "Status";
        colStatus.Width = 120;
        //
        // ctxPorts
        //
        ctxPorts.Items.AddRange(new ToolStripItem[] { ctxEdit, ctxRemove });
        ctxPorts.Name = "ctxPorts";
        ctxPorts.Size = new Size(140, 48);
        //
        // ctxEdit
        //
        ctxEdit.Name = "ctxEdit";
        ctxEdit.Size = new Size(139, 22);
        ctxEdit.Text = "Edit…";
        ctxEdit.Click += btnEdit_Click;
        //
        // ctxRemove
        //
        ctxRemove.Name = "ctxRemove";
        ctxRemove.Size = new Size(139, 22);
        ctxRemove.Text = "Remove";
        ctxRemove.Click += btnRemove_Click;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 314);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(524, 43);
        panFooter.TabIndex = 2;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnClose);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(424, 8);
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
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { lblHint });
        statusStrip.Location = new Point(0, 357);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(544, 22);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 3;
        //
        // lblHint
        //
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(200, 17);
        lblHint.Text = "Apache will listen on:";
        //
        // PortsForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(544, 379);
        Controls.Add(lvPorts);
        Controls.Add(panFooter);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        MinimumSize = new Size(520, 340);
        Name = "PortsForm";
        Padding = new Padding(10, 0, 10, 0);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Ports";

        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        ctxPorts.ResumeLayout(false);
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip toolStrip;
    private ToolStripButton btnAdd;
    private ToolStripButton btnEdit;
    private ToolStripSeparator tsSep1;
    private ToolStripButton btnRemove;
    private ListView lvPorts;
    private ColumnHeader colPort;
    private ColumnHeader colSsl;
    private ColumnHeader colKind;
    private ColumnHeader colStatus;
    private ContextMenuStrip ctxPorts;
    private ToolStripMenuItem ctxEdit;
    private ToolStripMenuItem ctxRemove;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnClose;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblHint;
}
