using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class PortDialog : Form
{
    // ── Results ───────────────────────────────────────────────────────────────
    public int PortValue { get; private set; }
    public bool IsSsl { get; private set; }
    public bool IsEnabled { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly AppConfig _config;
    private readonly PortEntry? _editingEntry;
    private bool _showSsl;

    private enum EditMode { AddExtra, EditExtra, EditPrimaryHttp, EditPrimaryHttps }
    private EditMode _mode;

    private PortDialog(AppConfig config, PortEntry? editingEntry = null)
    {
        InitializeComponent();
        _config = config;
        _editingEntry = editingEntry;
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    public static PortDialog ForAdd(AppConfig config)
    {
        var d = new PortDialog(config) { _mode = EditMode.AddExtra, _showSsl = true };
        d.Text = "Add Port";
        d.lblTitle.Text = "Add a port";
        d.lblSub.Text = "Add a new Apache listening port";
        d.HideRow(2);   // Enabled — new extra ports always active
        d.ShrinkBy(1);
        return d;
    }

    public static PortDialog ForEditPrimaryHttp(AppConfig config)
    {
        var d = new PortDialog(config) { _mode = EditMode.EditPrimaryHttp, _showSsl = false };
        d.Text = "Edit HTTP Port";
        d.lblTitle.Text = "Edit the HTTP port";
        d.lblSub.Text = "Change the primary Apache HTTP port";
        d.nudPort.Value = config.Apache.Port;
        d.HideRow(1);   // SSL
        d.HideRow(2);   // Enabled
        d.ShrinkBy(2);
        return d;
    }

    public static PortDialog ForEditPrimaryHttps(AppConfig config)
    {
        var d = new PortDialog(config) { _mode = EditMode.EditPrimaryHttps, _showSsl = true };
        d.Text = "Edit HTTPS Port";
        d.lblTitle.Text = "Edit the HTTPS port";
        d.lblSub.Text = "Change the SSL port and toggle HTTPS";
        d.nudPort.Value = config.Apache.SslPort;
        d.chkSsl.Text = "Enable SSL (HTTPS)";
        d.chkSsl.Checked = config.Apache.EnableSsl;
        d.HideRow(2);     // Enabled
        d.ShrinkBy(1);
        return d;
    }

    public static PortDialog ForEditExtra(AppConfig config, PortEntry entry)
    {
        var d = new PortDialog(config, entry) { _mode = EditMode.EditExtra, _showSsl = true };
        d.Text = "Edit Port";
        d.lblTitle.Text = "Edit the port";
        d.lblSub.Text = "Modify an existing extra port";
        d.nudPort.Value = entry.Port;
        d.chkSsl.Checked = entry.IsSsl;
        d.chkEnabled.Checked = entry.Enabled;
        return d;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void HideRow(int idx)
    {
        tblBody.RowStyles[idx] = new RowStyle(SizeType.Absolute, 0F);
        foreach (Control c in tblBody.Controls.OfType<Control>().ToList())
            if (tblBody.GetRow(c) == idx) c.Visible = false;
    }

    // Must match the RowStyle height used for every row of tblBody in the designer.
    private const int BodyRowHeight = 30;

    private void ShrinkBy(int hiddenRows)
        => ClientSize = new Size(ClientSize.Width, ClientSize.Height - hiddenRows * BodyRowHeight);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool HasConflict(int port)
    {
        bool skipHttp = _mode == EditMode.EditPrimaryHttp;
        bool skipHttps = _mode == EditMode.EditPrimaryHttps;

        if (!skipHttp && port == _config.Apache.Port)
        {
            MessageBox.Show($"Port {port} is already the primary HTTP port.", "Port Conflict",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if (!skipHttps && port == _config.Apache.SslPort)
        {
            MessageBox.Show($"Port {port} is already the primary HTTPS port.", "Port Conflict",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if (port == _config.Php.FcgiPort)
        {
            MessageBox.Show($"Port {port} is already used by PHP FastCGI.", "Port Conflict",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if ((_config.Apache.ExtraPorts ?? []).Any(p => p != _editingEntry && p.Port == port))
        {
            MessageBox.Show($"Port {port} is already in the port list.", "Port Conflict",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        return false;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void btnSave_Click(object sender, EventArgs e)
    {
        var port = (int)nudPort.Value;
        bool ssl = _showSsl && chkSsl.Checked;

        if (HasConflict(port)) return;

        // SSL cert required when enabling SSL on extra ports
        if (ssl && _mode is EditMode.AddExtra or EditMode.EditExtra &&
            (string.IsNullOrWhiteSpace(_config.Apache.SslCertFile) ||
             string.IsNullOrWhiteSpace(_config.Apache.SslKeyFile)))
        {
            MessageBox.Show(
                "SSL certificate and key must be configured in Settings → Apache before adding an SSL port.",
                "SSL Not Configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PortValue = port;
        IsSsl = ssl;
        IsEnabled = _mode == EditMode.EditExtra ? chkEnabled.Checked : true;
        DialogResult = DialogResult.OK;
    }

    private void btnCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;
}
