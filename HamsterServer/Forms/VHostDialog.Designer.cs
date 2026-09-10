namespace HamsterServer.Forms;

partial class VHostDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _tt?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        panHeader = new Panel();
        lblSub = new Label();
        lblTitle = new Label();
        grpDetails = new GroupBox();
        tblEditor = new TableLayoutPanel();
        lblEditorName = new Label();
        txtServerName = new TextBox();
        lblEditorAlias = new Label();
        txtServerAlias = new TextBox();
        lblEditorPort = new Label();
        cmbPort = new ComboBox();
        lblEditorFwk = new Label();
        cmbFramework = new ComboBox();
        lblEditorRoot = new Label();
        txtDocRoot = new TextBox();
        btnBrowse = new Button();
        lblDocNote = new Label();
        lblEditorEnable = new Label();
        chkEnabled = new CheckBox();
        grpProfile = new GroupBox();
        tblProfile = new TableLayoutPanel();
        lblProfileDescription = new Label();
        lblProfileRules = new Label();
        lblProfileStatus = new Label();
        btnEnableExtensions = new Button();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnCancel = new Button();
        btnSave = new Button();

        panHeader.SuspendLayout();
        grpDetails.SuspendLayout();
        tblEditor.SuspendLayout();
        grpProfile.SuspendLayout();
        tblProfile.SuspendLayout();
        panFooter.SuspendLayout();
        flpFooter.SuspendLayout();
        SuspendLayout();

        //
        // panHeader
        //
        panHeader.Controls.Add(lblSub);
        panHeader.Controls.Add(lblTitle);
        panHeader.Dock = DockStyle.Top;
        panHeader.Location = new Point(10, 10);
        panHeader.Name = "panHeader";
        panHeader.Padding = new Padding(0, 0, 0, 6);
        panHeader.Size = new Size(544, 46);
        panHeader.TabIndex = 0;
        //
        // lblTitle
        //
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(544, 20);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Add Virtual Host";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblSub
        //
        lblSub.Dock = DockStyle.Top;
        lblSub.ForeColor = SystemColors.GrayText;
        lblSub.Location = new Point(0, 20);
        lblSub.Name = "lblSub";
        lblSub.Size = new Size(544, 20);
        lblSub.TabIndex = 1;
        lblSub.Text = "Configure a new Apache virtual host";
        lblSub.TextAlign = ContentAlignment.MiddleLeft;
        //
        // grpDetails
        //
        grpDetails.Controls.Add(tblEditor);
        grpDetails.Dock = DockStyle.Fill;
        grpDetails.Location = new Point(10, 56);
        grpDetails.Name = "grpDetails";
        grpDetails.Padding = new Padding(8, 4, 8, 6);
        grpDetails.Size = new Size(544, 253);
        grpDetails.TabIndex = 1;
        grpDetails.TabStop = false;
        grpDetails.Text = "Details";
        //
        // tblEditor
        //
        tblEditor.ColumnCount = 3;
        tblEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tblEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblEditor.Controls.Add(lblEditorName, 0, 0);
        tblEditor.Controls.Add(txtServerName, 1, 0);
        tblEditor.Controls.Add(lblEditorAlias, 0, 1);
        tblEditor.Controls.Add(txtServerAlias, 1, 1);
        tblEditor.Controls.Add(lblEditorPort, 0, 2);
        tblEditor.Controls.Add(cmbPort, 1, 2);
        tblEditor.Controls.Add(lblEditorFwk, 0, 3);
        tblEditor.Controls.Add(cmbFramework, 1, 3);
        tblEditor.Controls.Add(lblEditorRoot, 0, 4);
        tblEditor.Controls.Add(txtDocRoot, 1, 4);
        tblEditor.Controls.Add(btnBrowse, 2, 4);
        tblEditor.Controls.Add(lblDocNote, 1, 5);
        tblEditor.Controls.Add(lblEditorEnable, 0, 6);
        tblEditor.Controls.Add(chkEnabled, 1, 6);
        tblEditor.Dock = DockStyle.Fill;
        tblEditor.Location = new Point(8, 20);
        tblEditor.Name = "tblEditor";
        tblEditor.RowCount = 7;
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tblEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblEditor.Size = new Size(528, 227);
        tblEditor.TabIndex = 0;
        //
        // lblEditorName
        //
        lblEditorName.AutoSize = true;
        lblEditorName.Dock = DockStyle.Fill;
        lblEditorName.Location = new Point(3, 0);
        lblEditorName.Name = "lblEditorName";
        lblEditorName.Size = new Size(124, 30);
        lblEditorName.TabIndex = 0;
        lblEditorName.Text = "Server name *";
        lblEditorName.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtServerName
        //
        txtServerName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        tblEditor.SetColumnSpan(txtServerName, 2);
        txtServerName.Location = new Point(133, 4);
        txtServerName.Name = "txtServerName";
        txtServerName.Size = new Size(392, 23);
        txtServerName.TabIndex = 1;
        //
        // lblEditorAlias
        //
        lblEditorAlias.AutoSize = true;
        lblEditorAlias.Dock = DockStyle.Fill;
        lblEditorAlias.Location = new Point(3, 30);
        lblEditorAlias.Name = "lblEditorAlias";
        lblEditorAlias.Size = new Size(124, 30);
        lblEditorAlias.TabIndex = 2;
        lblEditorAlias.Text = "Server aliases";
        lblEditorAlias.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtServerAlias
        //
        txtServerAlias.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        tblEditor.SetColumnSpan(txtServerAlias, 2);
        txtServerAlias.Location = new Point(133, 34);
        txtServerAlias.Name = "txtServerAlias";
        txtServerAlias.Size = new Size(392, 23);
        txtServerAlias.TabIndex = 3;
        //
        // lblEditorPort
        //
        lblEditorPort.AutoSize = true;
        lblEditorPort.Dock = DockStyle.Fill;
        lblEditorPort.Location = new Point(3, 60);
        lblEditorPort.Name = "lblEditorPort";
        lblEditorPort.Size = new Size(124, 30);
        lblEditorPort.TabIndex = 4;
        lblEditorPort.Text = "Port";
        lblEditorPort.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cmbPort
        //
        cmbPort.Anchor = AnchorStyles.Left;
        cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPort.FormattingEnabled = true;
        cmbPort.Location = new Point(133, 64);
        cmbPort.Name = "cmbPort";
        cmbPort.Size = new Size(230, 23);
        cmbPort.TabIndex = 5;
        //
        // lblEditorFwk
        //
        lblEditorFwk.AutoSize = true;
        lblEditorFwk.Dock = DockStyle.Fill;
        lblEditorFwk.Location = new Point(3, 90);
        lblEditorFwk.Name = "lblEditorFwk";
        lblEditorFwk.Size = new Size(124, 30);
        lblEditorFwk.TabIndex = 6;
        lblEditorFwk.Text = "Framework";
        lblEditorFwk.TextAlign = ContentAlignment.MiddleLeft;
        //
        // cmbFramework
        //
        cmbFramework.Anchor = AnchorStyles.Left;
        cmbFramework.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFramework.FormattingEnabled = true;
        cmbFramework.Location = new Point(133, 94);
        cmbFramework.Name = "cmbFramework";
        cmbFramework.Size = new Size(230, 23);
        cmbFramework.TabIndex = 7;
        cmbFramework.SelectedIndexChanged += cmbFramework_SelectedIndexChanged;
        //
        // lblEditorRoot
        //
        lblEditorRoot.AutoSize = true;
        lblEditorRoot.Dock = DockStyle.Fill;
        lblEditorRoot.Location = new Point(3, 120);
        lblEditorRoot.Name = "lblEditorRoot";
        lblEditorRoot.Size = new Size(124, 30);
        lblEditorRoot.TabIndex = 8;
        lblEditorRoot.Text = "Document root *";
        lblEditorRoot.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtDocRoot
        //
        txtDocRoot.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtDocRoot.Location = new Point(133, 124);
        txtDocRoot.Name = "txtDocRoot";
        txtDocRoot.Size = new Size(296, 23);
        txtDocRoot.TabIndex = 9;
        //
        // btnBrowse
        //
        btnBrowse.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnBrowse.Location = new Point(435, 123);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(84, 25);
        btnBrowse.TabIndex = 10;
        btnBrowse.Text = "Browse…";
        btnBrowse.UseVisualStyleBackColor = true;
        btnBrowse.Click += btnBrowse_Click;
        //
        // lblDocNote
        //
        tblEditor.SetColumnSpan(lblDocNote, 2);
        lblDocNote.Dock = DockStyle.Fill;
        lblDocNote.ForeColor = SystemColors.GrayText;
        lblDocNote.Location = new Point(133, 150);
        lblDocNote.Name = "lblDocNote";
        lblDocNote.Size = new Size(392, 36);
        lblDocNote.TabIndex = 11;
        lblDocNote.Text = "Enter the full path to the document root folder.";
        //
        // lblEditorEnable
        //
        lblEditorEnable.AutoSize = true;
        lblEditorEnable.Dock = DockStyle.Fill;
        lblEditorEnable.Location = new Point(3, 186);
        lblEditorEnable.Name = "lblEditorEnable";
        lblEditorEnable.Size = new Size(124, 30);
        lblEditorEnable.TabIndex = 12;
        lblEditorEnable.Text = "Status";
        lblEditorEnable.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkEnabled
        //
        chkEnabled.Anchor = AnchorStyles.Left;
        chkEnabled.AutoSize = true;
        chkEnabled.Checked = true;
        chkEnabled.CheckState = CheckState.Checked;
        tblEditor.SetColumnSpan(chkEnabled, 2);
        chkEnabled.Location = new Point(133, 191);
        chkEnabled.Name = "chkEnabled";
        chkEnabled.Size = new Size(200, 19);
        chkEnabled.TabIndex = 13;
        chkEnabled.Text = "Enabled";
        chkEnabled.UseVisualStyleBackColor = true;
        //
        // grpProfile
        //
        grpProfile.Controls.Add(tblProfile);
        grpProfile.Dock = DockStyle.Bottom;
        grpProfile.Location = new Point(10, 309);
        grpProfile.Name = "grpProfile";
        grpProfile.Padding = new Padding(8, 4, 8, 6);
        grpProfile.Size = new Size(544, 126);
        grpProfile.TabIndex = 2;
        grpProfile.TabStop = false;
        grpProfile.Text = "Profile settings";
        //
        // tblProfile
        //
        tblProfile.ColumnCount = 2;
        tblProfile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblProfile.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        tblProfile.Controls.Add(lblProfileDescription, 0, 0);
        tblProfile.Controls.Add(lblProfileRules, 0, 1);
        tblProfile.Controls.Add(lblProfileStatus, 0, 2);
        tblProfile.Controls.Add(btnEnableExtensions, 1, 2);
        tblProfile.Dock = DockStyle.Fill;
        tblProfile.Location = new Point(8, 20);
        tblProfile.Name = "tblProfile";
        tblProfile.RowCount = 3;
        tblProfile.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        tblProfile.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblProfile.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblProfile.Size = new Size(528, 100);
        tblProfile.TabIndex = 0;
        //
        // lblProfileDescription
        //
        tblProfile.SetColumnSpan(lblProfileDescription, 2);
        lblProfileDescription.Dock = DockStyle.Fill;
        lblProfileDescription.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblProfileDescription.Location = new Point(3, 0);
        lblProfileDescription.Name = "lblProfileDescription";
        lblProfileDescription.Size = new Size(522, 34);
        lblProfileDescription.TabIndex = 0;
        lblProfileDescription.Text = "Serve a custom PHP document root.";
        //
        // lblProfileRules
        //
        tblProfile.SetColumnSpan(lblProfileRules, 2);
        lblProfileRules.Dock = DockStyle.Fill;
        lblProfileRules.ForeColor = SystemColors.GrayText;
        lblProfileRules.Location = new Point(3, 34);
        lblProfileRules.Name = "lblProfileRules";
        lblProfileRules.Size = new Size(522, 26);
        lblProfileRules.TabIndex = 1;
        lblProfileRules.Text = "Web root: Selected directory    Routing: Direct file handling";
        //
        // lblProfileStatus
        //
        lblProfileStatus.Dock = DockStyle.Fill;
        lblProfileStatus.Location = new Point(3, 60);
        lblProfileStatus.Name = "lblProfileStatus";
        lblProfileStatus.Size = new Size(380, 40);
        lblProfileStatus.TabIndex = 2;
        lblProfileStatus.Text = "Choose a directory to validate the project.";
        //
        // btnEnableExtensions
        //
        btnEnableExtensions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnEnableExtensions.Location = new Point(389, 66);
        btnEnableExtensions.Name = "btnEnableExtensions";
        btnEnableExtensions.Size = new Size(136, 27);
        btnEnableExtensions.TabIndex = 3;
        btnEnableExtensions.Text = "Enable extensions";
        btnEnableExtensions.UseVisualStyleBackColor = true;
        btnEnableExtensions.Visible = false;
        btnEnableExtensions.Click += btnEnableExtensions_Click;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 435);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(544, 43);
        panFooter.TabIndex = 3;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnCancel);
        flpFooter.Controls.Add(btnSave);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(344, 8);
        flpFooter.Name = "flpFooter";
        flpFooter.Size = new Size(200, 35);
        flpFooter.TabIndex = 0;
        flpFooter.WrapContents = false;
        //
        // btnCancel
        //
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(107, 3);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 27);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        //
        // btnSave
        //
        btnSave.Location = new Point(11, 3);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(90, 27);
        btnSave.TabIndex = 0;
        btnSave.Text = "OK";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        //
        // VHostDialog
        //
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(564, 488);
        Controls.Add(grpDetails);
        Controls.Add(grpProfile);
        Controls.Add(panFooter);
        Controls.Add(panHeader);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "VHostDialog";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Virtual Host";

        panHeader.ResumeLayout(false);
        grpDetails.ResumeLayout(false);
        tblEditor.ResumeLayout(false);
        tblEditor.PerformLayout();
        grpProfile.ResumeLayout(false);
        tblProfile.ResumeLayout(false);
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel panHeader;
    private Label lblTitle;
    private Label lblSub;
    private GroupBox grpDetails;
    private TableLayoutPanel tblEditor;
    private Label lblEditorName;
    private TextBox txtServerName;
    private Label lblEditorAlias;
    private TextBox txtServerAlias;
    private Label lblEditorPort;
    private ComboBox cmbPort;
    private Label lblEditorFwk;
    private ComboBox cmbFramework;
    private Label lblEditorRoot;
    private TextBox txtDocRoot;
    private Button btnBrowse;
    private Label lblDocNote;
    private Label lblEditorEnable;
    private CheckBox chkEnabled;
    private GroupBox grpProfile;
    private TableLayoutPanel tblProfile;
    private Label lblProfileDescription;
    private Label lblProfileRules;
    private Label lblProfileStatus;
    private Button btnEnableExtensions;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnCancel;
    private Button btnSave;
}
