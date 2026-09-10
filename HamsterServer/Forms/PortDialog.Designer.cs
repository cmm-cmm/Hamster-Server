namespace HamsterServer.Forms;

partial class PortDialog
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

        panHeader = new Panel();
        lblSub = new Label();
        lblTitle = new Label();
        grpPort = new GroupBox();
        tblBody = new TableLayoutPanel();
        lblPortNum = new Label();
        nudPort = new NumericUpDown();
        lblSsl = new Label();
        chkSsl = new CheckBox();
        lblEnabled = new Label();
        chkEnabled = new CheckBox();
        panFooter = new Panel();
        flpFooter = new FlowLayoutPanel();
        btnCancel = new Button();
        btnSave = new Button();

        panHeader.SuspendLayout();
        grpPort.SuspendLayout();
        tblBody.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudPort).BeginInit();
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
        panHeader.Size = new Size(384, 46);
        panHeader.TabIndex = 0;
        //
        // lblTitle
        //
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(384, 20);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Add Port";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblSub
        //
        lblSub.Dock = DockStyle.Top;
        lblSub.ForeColor = SystemColors.GrayText;
        lblSub.Location = new Point(0, 20);
        lblSub.Name = "lblSub";
        lblSub.Size = new Size(384, 20);
        lblSub.TabIndex = 1;
        lblSub.Text = "Add a new Apache listening port";
        lblSub.TextAlign = ContentAlignment.MiddleLeft;
        //
        // grpPort
        //
        grpPort.Controls.Add(tblBody);
        grpPort.Dock = DockStyle.Fill;
        grpPort.Location = new Point(10, 56);
        grpPort.Name = "grpPort";
        grpPort.Padding = new Padding(8, 4, 8, 6);
        grpPort.Size = new Size(384, 121);
        grpPort.TabIndex = 1;
        grpPort.TabStop = false;
        grpPort.Text = "Port";
        //
        // tblBody
        //
        tblBody.ColumnCount = 2;
        tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblBody.Controls.Add(lblPortNum, 0, 0);
        tblBody.Controls.Add(nudPort, 1, 0);
        tblBody.Controls.Add(lblSsl, 0, 1);
        tblBody.Controls.Add(chkSsl, 1, 1);
        tblBody.Controls.Add(lblEnabled, 0, 2);
        tblBody.Controls.Add(chkEnabled, 1, 2);
        tblBody.Dock = DockStyle.Fill;
        tblBody.Location = new Point(8, 20);
        tblBody.Name = "tblBody";
        tblBody.RowCount = 3;
        tblBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblBody.Size = new Size(368, 95);
        tblBody.TabIndex = 0;
        //
        // lblPortNum
        //
        lblPortNum.AutoSize = true;
        lblPortNum.Dock = DockStyle.Fill;
        lblPortNum.Location = new Point(3, 0);
        lblPortNum.Name = "lblPortNum";
        lblPortNum.Size = new Size(124, 30);
        lblPortNum.TabIndex = 0;
        lblPortNum.Text = "Port number";
        lblPortNum.TextAlign = ContentAlignment.MiddleLeft;
        //
        // nudPort
        //
        nudPort.Anchor = AnchorStyles.Left;
        nudPort.Location = new Point(133, 4);
        nudPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        nudPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudPort.Name = "nudPort";
        nudPort.Size = new Size(100, 23);
        nudPort.TabIndex = 1;
        nudPort.Value = new decimal(new int[] { 8080, 0, 0, 0 });
        //
        // lblSsl
        //
        lblSsl.AutoSize = true;
        lblSsl.Dock = DockStyle.Fill;
        lblSsl.Location = new Point(3, 30);
        lblSsl.Name = "lblSsl";
        lblSsl.Size = new Size(124, 30);
        lblSsl.TabIndex = 2;
        lblSsl.Text = "Protocol";
        lblSsl.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkSsl
        //
        chkSsl.Anchor = AnchorStyles.Left;
        chkSsl.AutoSize = true;
        chkSsl.Location = new Point(133, 35);
        chkSsl.Name = "chkSsl";
        chkSsl.Size = new Size(180, 19);
        chkSsl.TabIndex = 3;
        chkSsl.Text = "Use SSL (HTTPS)";
        chkSsl.UseVisualStyleBackColor = true;
        //
        // lblEnabled
        //
        lblEnabled.AutoSize = true;
        lblEnabled.Dock = DockStyle.Fill;
        lblEnabled.Location = new Point(3, 60);
        lblEnabled.Name = "lblEnabled";
        lblEnabled.Size = new Size(124, 30);
        lblEnabled.TabIndex = 4;
        lblEnabled.Text = "Status";
        lblEnabled.TextAlign = ContentAlignment.MiddleLeft;
        //
        // chkEnabled
        //
        chkEnabled.Anchor = AnchorStyles.Left;
        chkEnabled.AutoSize = true;
        chkEnabled.Checked = true;
        chkEnabled.CheckState = CheckState.Checked;
        chkEnabled.Location = new Point(133, 65);
        chkEnabled.Name = "chkEnabled";
        chkEnabled.Size = new Size(180, 19);
        chkEnabled.TabIndex = 5;
        chkEnabled.Text = "Enabled";
        chkEnabled.UseVisualStyleBackColor = true;
        //
        // panFooter
        //
        panFooter.Controls.Add(flpFooter);
        panFooter.Dock = DockStyle.Bottom;
        panFooter.Location = new Point(10, 177);
        panFooter.Name = "panFooter";
        panFooter.Padding = new Padding(0, 8, 0, 0);
        panFooter.Size = new Size(384, 43);
        panFooter.TabIndex = 2;
        //
        // flpFooter
        //
        flpFooter.Controls.Add(btnCancel);
        flpFooter.Controls.Add(btnSave);
        flpFooter.Dock = DockStyle.Right;
        flpFooter.FlowDirection = FlowDirection.RightToLeft;
        flpFooter.Location = new Point(184, 8);
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
        // PortDialog
        //
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(404, 230);
        Controls.Add(grpPort);
        Controls.Add(panFooter);
        Controls.Add(panHeader);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PortDialog";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Port";

        panHeader.ResumeLayout(false);
        grpPort.ResumeLayout(false);
        tblBody.ResumeLayout(false);
        tblBody.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudPort).EndInit();
        panFooter.ResumeLayout(false);
        flpFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel panHeader;
    private Label lblTitle;
    private Label lblSub;
    private GroupBox grpPort;
    private TableLayoutPanel tblBody;
    private Label lblPortNum;
    private NumericUpDown nudPort;
    private Label lblSsl;
    private CheckBox chkSsl;
    private Label lblEnabled;
    private CheckBox chkEnabled;
    private Panel panFooter;
    private FlowLayoutPanel flpFooter;
    private Button btnCancel;
    private Button btnSave;
}
