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

        tabPorts = new TabControl();
        tabApache = new TabPage();
        lvPorts = new ListView();
        colPort = new ColumnHeader();
        colSsl = new ColumnHeader();
        colKind = new ColumnHeader();
        colStatus = new ColumnHeader();
        colMachine = new ColumnHeader();
        lblApacheIntro = new Label();
        toolStrip = new ToolStrip();
        btnAdd = new ToolStripButton();
        btnEdit = new ToolStripButton();
        tsSep1 = new ToolStripSeparator();
        btnRemove = new ToolStripButton();
        tsSep2 = new ToolStripSeparator();
        btnRecheck = new ToolStripButton();
        tabSystem = new TabPage();
        lvSystem = new ListView();
        colSysPort = new ColumnHeader();
        colSysAddress = new ColumnHeader();
        colSysProcess = new ColumnHeader();
        colSysPid = new ColumnHeader();
        colSysUse = new ColumnHeader();
        lblSystemIntro = new Label();
        tsSystem = new ToolStrip();
        btnSysRefresh = new ToolStripButton();
        tsSysSep1 = new ToolStripSeparator();
        lblSysFilter = new ToolStripLabel();
        txtSysFilter = new ToolStripTextBox();
        btnSysRelevant = new ToolStripButton();
        tsSysSep2 = new ToolStripSeparator();
        lblSysCount = new ToolStripLabel();
        ctxPorts = new ContextMenuStrip(components);
        ctxEdit = new ToolStripMenuItem();
        ctxRemove = new ToolStripMenuItem();
        ctxSystem = new ContextMenuStrip(components);
        ctxSysCopy = new ToolStripMenuItem();
        ctxSysOpenLocation = new ToolStripMenuItem();
        ctxSysBrowse = new ToolStripMenuItem();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnClose = new Button();
        statusStrip = new StatusStrip();
        lblHint = new ToolStripStatusLabel();

        tabPorts.SuspendLayout();
        tabApache.SuspendLayout();
        toolStrip.SuspendLayout();
        tabSystem.SuspendLayout();
        tsSystem.SuspendLayout();
        ctxPorts.SuspendLayout();
        ctxSystem.SuspendLayout();
        panFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        //
        // tabPorts
        //
        tabPorts.Controls.Add(tabApache);
        tabPorts.Controls.Add(tabSystem);
        tabPorts.Dock = DockStyle.Fill;
        tabPorts.Location = new Point(10, 10);
        tabPorts.Name = "tabPorts";
        tabPorts.Padding = new Point(12, 5);
        tabPorts.SelectedIndex = 0;
        tabPorts.Size = new Size(764, 408);
        tabPorts.TabIndex = 0;
        tabPorts.SelectedIndexChanged += tabPorts_SelectedIndexChanged;

        // ═══ Apache ports tab ═════════════════════════════════════════════════
        //
        // tabApache
        //
        tabApache.Controls.Add(lvPorts);
        tabApache.Controls.Add(lblApacheIntro);
        tabApache.Controls.Add(toolStrip);
        tabApache.Location = new Point(4, 28);
        tabApache.Name = "tabApache";
        tabApache.Padding = new Padding(6);
        tabApache.Size = new Size(756, 376);
        tabApache.TabIndex = 0;
        tabApache.Text = "Apache ports";
        tabApache.UseVisualStyleBackColor = true;
        //
        // toolStrip
        //
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, tsSep1, btnRemove, tsSep2, btnRecheck });
        toolStrip.Location = new Point(6, 6);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(744, 25);
        toolStrip.TabIndex = 0;
        //
        // btnAdd
        //
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(45, 22);
        btnAdd.Text = "Add…";
        btnAdd.ToolTipText = "Add a listening port";
        btnAdd.Click += btnAdd_Click;
        //
        // btnEdit
        //
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
        btnRemove.Name = "btnRemove";
        btnRemove.Size = new Size(60, 22);
        btnRemove.Text = "Remove";
        btnRemove.ToolTipText = "Remove the selected extra port (primary ports cannot be removed)";
        btnRemove.Click += btnRemove_Click;
        //
        // tsSep2
        //
        tsSep2.Name = "tsSep2";
        tsSep2.Size = new Size(6, 25);
        //
        // btnRecheck
        //
        btnRecheck.Name = "btnRecheck";
        btnRecheck.Size = new Size(110, 22);
        btnRecheck.Text = "Check availability";
        btnRecheck.ToolTipText = "Look again at which ports are free on this computer";
        btnRecheck.Click += btnSysRefresh_Click;
        //
        // lblApacheIntro
        //
        lblApacheIntro.Dock = DockStyle.Top;
        lblApacheIntro.ForeColor = SystemColors.GrayText;
        lblApacheIntro.Location = new Point(6, 31);
        lblApacheIntro.Name = "lblApacheIntro";
        lblApacheIntro.Padding = new Padding(2, 4, 2, 4);
        lblApacheIntro.Size = new Size(744, 26);
        lblApacheIntro.TabIndex = 1;
        lblApacheIntro.Text = "Ports Apache listens on. Tick a port to enable it; the last column says whether it is free on this computer.";
        //
        // lvPorts
        //
        lvPorts.CheckBoxes = true;
        lvPorts.Columns.AddRange(new ColumnHeader[] { colPort, colSsl, colKind, colStatus, colMachine });
        lvPorts.ContextMenuStrip = ctxPorts;
        lvPorts.Dock = DockStyle.Fill;
        lvPorts.FullRowSelect = true;
        lvPorts.GridLines = true;
        lvPorts.HideSelection = false;
        lvPorts.Location = new Point(6, 57);
        lvPorts.MultiSelect = false;
        lvPorts.Name = "lvPorts";
        lvPorts.Size = new Size(744, 313);
        lvPorts.TabIndex = 2;
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
        colPort.Width = 90;
        //
        // colSsl
        //
        colSsl.Text = "SSL";
        colSsl.TextAlign = HorizontalAlignment.Center;
        colSsl.Width = 50;
        //
        // colKind
        //
        colKind.Text = "Kind";
        colKind.Width = 140;
        //
        // colStatus
        //
        colStatus.Text = "Status";
        colStatus.Width = 90;
        //
        // colMachine
        //
        colMachine.Text = "On this computer";
        colMachine.Width = 300;

        // ═══ System ports tab ═════════════════════════════════════════════════
        //
        // tabSystem
        //
        tabSystem.Controls.Add(lvSystem);
        tabSystem.Controls.Add(lblSystemIntro);
        tabSystem.Controls.Add(tsSystem);
        tabSystem.Location = new Point(4, 28);
        tabSystem.Name = "tabSystem";
        tabSystem.Padding = new Padding(6);
        tabSystem.Size = new Size(756, 376);
        tabSystem.TabIndex = 1;
        tabSystem.Text = "System ports";
        tabSystem.UseVisualStyleBackColor = true;
        //
        // tsSystem
        //
        tsSystem.GripStyle = ToolStripGripStyle.Hidden;
        tsSystem.Items.AddRange(new ToolStripItem[] { btnSysRefresh, tsSysSep1, lblSysFilter, txtSysFilter, btnSysRelevant, tsSysSep2, lblSysCount });
        tsSystem.Location = new Point(6, 6);
        tsSystem.Name = "tsSystem";
        tsSystem.Size = new Size(744, 25);
        tsSystem.TabIndex = 0;
        //
        // btnSysRefresh
        //
        btnSysRefresh.Name = "btnSysRefresh";
        btnSysRefresh.Size = new Size(66, 22);
        btnSysRefresh.Text = "Refresh";
        btnSysRefresh.ToolTipText = "Read the listening ports again";
        btnSysRefresh.Click += btnSysRefresh_Click;
        //
        // tsSysSep1
        //
        tsSysSep1.Name = "tsSysSep1";
        tsSysSep1.Size = new Size(6, 25);
        //
        // lblSysFilter
        //
        lblSysFilter.Name = "lblSysFilter";
        lblSysFilter.Size = new Size(36, 22);
        lblSysFilter.Text = "Filter:";
        //
        // txtSysFilter
        //
        txtSysFilter.Name = "txtSysFilter";
        txtSysFilter.Size = new Size(140, 25);
        txtSysFilter.ToolTipText = "Port number, process name or service";
        txtSysFilter.TextChanged += txtSysFilter_TextChanged;
        //
        // btnSysRelevant
        //
        btnSysRelevant.CheckOnClick = true;
        btnSysRelevant.Name = "btnSysRelevant";
        btnSysRelevant.Size = new Size(160, 22);
        btnSysRelevant.Text = "Hamster && conflicts only";
        btnSysRelevant.ToolTipText = "Show only ports Hamster Server uses or is configured to use";
        btnSysRelevant.CheckedChanged += txtSysFilter_TextChanged;
        //
        // tsSysSep2
        //
        tsSysSep2.Name = "tsSysSep2";
        tsSysSep2.Size = new Size(6, 25);
        //
        // lblSysCount
        //
        lblSysCount.ForeColor = SystemColors.GrayText;
        lblSysCount.Name = "lblSysCount";
        lblSysCount.Size = new Size(120, 22);
        lblSysCount.Text = "";
        //
        // lblSystemIntro
        //
        lblSystemIntro.Dock = DockStyle.Top;
        lblSystemIntro.ForeColor = SystemColors.GrayText;
        lblSystemIntro.Location = new Point(6, 31);
        lblSystemIntro.Name = "lblSystemIntro";
        lblSystemIntro.Padding = new Padding(2, 4, 2, 4);
        lblSystemIntro.Size = new Size(744, 26);
        lblSystemIntro.TabIndex = 1;
        lblSystemIntro.Text = "Every TCP port listening on this computer and the program behind it. Red rows hold a port Hamster Server needs.";
        //
        // lvSystem
        //
        lvSystem.Columns.AddRange(new ColumnHeader[] { colSysPort, colSysAddress, colSysProcess, colSysPid, colSysUse });
        lvSystem.ContextMenuStrip = ctxSystem;
        lvSystem.Dock = DockStyle.Fill;
        lvSystem.FullRowSelect = true;
        lvSystem.GridLines = true;
        lvSystem.HideSelection = false;
        lvSystem.Location = new Point(6, 57);
        lvSystem.MultiSelect = false;
        lvSystem.Name = "lvSystem";
        lvSystem.ShowItemToolTips = true;
        lvSystem.Size = new Size(744, 313);
        lvSystem.TabIndex = 2;
        lvSystem.UseCompatibleStateImageBehavior = false;
        lvSystem.View = View.Details;
        lvSystem.ColumnClick += lvSystem_ColumnClick;
        lvSystem.Resize += lvSystem_Resize;
        //
        // colSysPort
        //
        colSysPort.Text = "Port";
        colSysPort.Width = 80;
        //
        // colSysAddress
        //
        colSysAddress.Text = "Listening on";
        colSysAddress.Width = 150;
        //
        // colSysProcess
        //
        colSysProcess.Text = "Program";
        colSysProcess.Width = 150;
        //
        // colSysPid
        //
        colSysPid.Text = "PID";
        colSysPid.TextAlign = HorizontalAlignment.Right;
        colSysPid.Width = 60;
        //
        // colSysUse
        //
        colSysUse.Text = "Used for";
        colSysUse.Width = 280;
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
        // ctxSystem
        //
        ctxSystem.Items.AddRange(new ToolStripItem[] { ctxSysBrowse, ctxSysOpenLocation, ctxSysCopy });
        ctxSystem.Name = "ctxSystem";
        ctxSystem.Size = new Size(220, 70);
        ctxSystem.Opening += ctxSystem_Opening;
        //
        // ctxSysBrowse
        //
        ctxSysBrowse.Name = "ctxSysBrowse";
        ctxSysBrowse.Size = new Size(219, 22);
        ctxSysBrowse.Text = "Open in browser";
        ctxSysBrowse.Click += ctxSysBrowse_Click;
        //
        // ctxSysOpenLocation
        //
        ctxSysOpenLocation.Name = "ctxSysOpenLocation";
        ctxSysOpenLocation.Size = new Size(219, 22);
        ctxSysOpenLocation.Text = "Open program folder";
        ctxSysOpenLocation.Click += ctxSysOpenLocation_Click;
        //
        // ctxSysCopy
        //
        ctxSysCopy.Name = "ctxSysCopy";
        ctxSysCopy.Size = new Size(219, 22);
        ctxSysCopy.Text = "Copy details";
        ctxSysCopy.Click += ctxSysCopy_Click;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 418);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(764, 43);
        panFooter.TabIndex = 1;
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
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { lblHint });
        statusStrip.Location = new Point(0, 461);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(784, 22);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 2;
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
        ClientSize = new Size(784, 483);
        Controls.Add(tabPorts);
        Controls.Add(panFooter);
        Controls.Add(statusStrip);
        MinimumSize = new Size(640, 400);
        Name = "PortsForm";
        Padding = new Padding(10, 10, 10, 0);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Ports";

        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        tabApache.ResumeLayout(false);
        tabApache.PerformLayout();
        tsSystem.ResumeLayout(false);
        tsSystem.PerformLayout();
        tabSystem.ResumeLayout(false);
        tabSystem.PerformLayout();
        tabPorts.ResumeLayout(false);
        ctxPorts.ResumeLayout(false);
        ctxSystem.ResumeLayout(false);
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TabControl tabPorts;
    private TabPage tabApache;
    private ToolStrip toolStrip;
    private ToolStripButton btnAdd;
    private ToolStripButton btnEdit;
    private ToolStripSeparator tsSep1;
    private ToolStripButton btnRemove;
    private ToolStripSeparator tsSep2;
    private ToolStripButton btnRecheck;
    private Label lblApacheIntro;
    private ListView lvPorts;
    private ColumnHeader colPort;
    private ColumnHeader colSsl;
    private ColumnHeader colKind;
    private ColumnHeader colStatus;
    private ColumnHeader colMachine;
    private TabPage tabSystem;
    private ToolStrip tsSystem;
    private ToolStripButton btnSysRefresh;
    private ToolStripSeparator tsSysSep1;
    private ToolStripLabel lblSysFilter;
    private ToolStripTextBox txtSysFilter;
    private ToolStripButton btnSysRelevant;
    private ToolStripSeparator tsSysSep2;
    private ToolStripLabel lblSysCount;
    private Label lblSystemIntro;
    private ListView lvSystem;
    private ColumnHeader colSysPort;
    private ColumnHeader colSysAddress;
    private ColumnHeader colSysProcess;
    private ColumnHeader colSysPid;
    private ColumnHeader colSysUse;
    private ContextMenuStrip ctxPorts;
    private ToolStripMenuItem ctxEdit;
    private ToolStripMenuItem ctxRemove;
    private ContextMenuStrip ctxSystem;
    private ToolStripMenuItem ctxSysBrowse;
    private ToolStripMenuItem ctxSysOpenLocation;
    private ToolStripMenuItem ctxSysCopy;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnClose;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblHint;
}
