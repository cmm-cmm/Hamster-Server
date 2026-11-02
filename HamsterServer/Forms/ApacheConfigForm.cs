using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class ApacheConfigForm : Form
{
    private readonly AppConfig _config;
    private readonly string _rootDir;
    private readonly ServerManager _server;
    private readonly string _httpdConfPath;
    private readonly string _vhostsDir;

    public bool ConfigChanged { get; private set; }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_SETREDRAW = 0x000B;

    public ApacheConfigForm(AppConfig config, string rootDir, ServerManager server)
    {
        InitializeComponent();
        _config = config;
        _rootDir = rootDir;
        _server = server;
        _httpdConfPath = Path.Combine(rootDir, "config", "httpd.conf");
        _vhostsDir = Path.Combine(rootDir, "config", "vhosts");

        rtbHttpdConf.Font = UiTheme.CreateMonospaceFont();
        rtbVHostContent.Font = UiTheme.CreateMonospaceFont();
        txtCustomDirectives.Font = UiTheme.CreateMonospaceFont();

        txtCustomDirectives.Text = _config.Apache.CustomDirectives ?? "";
        LoadHttpdConf();
        LoadVHostFiles();

        Load += (_, _) =>
        {
            if (splitVHost.Width > splitVHost.Panel1MinSize + splitVHost.Panel2MinSize + 10)
                splitVHost.SplitterDistance = Math.Min(220, splitVHost.Width - splitVHost.Panel2MinSize - 10);
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    // Both files are rewritten by every Apache start, which can be running on the thread pool while
    // this dialog is open. An unguarded read that lost that race threw out of the constructor —
    // the right-click "Configure…" then surfaced as the generic crash box instead of a dialog.
    private static string? TryReadAllText(string path, out string? error)
    {
        error = null;
        try { return File.ReadAllText(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            return null;
        }
    }

    private void LoadHttpdConf()
    {
        if (!File.Exists(_httpdConfPath))
        {
            rtbHttpdConf.Text = "(httpd.conf has not been generated yet — start Apache first)";
            return;
        }
        var text = TryReadAllText(_httpdConfPath, out var error);
        if (text is null)
        {
            rtbHttpdConf.Text = $"(httpd.conf could not be read: {error} — click Reload)";
            return;
        }
        rtbHttpdConf.Text = text;
        ApplySyntaxHighlight(rtbHttpdConf);
    }

    private void LoadVHostFiles()
    {
        lstVHostFiles.Items.Clear();
        rtbVHostContent.Clear();
        if (!Directory.Exists(_vhostsDir)) return;
        try
        {
            foreach (var f in Directory.GetFiles(_vhostsDir, "*.conf").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                lstVHostFiles.Items.Add(Path.GetFileName(f));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            rtbVHostContent.Text = $"(vhosts folder could not be read: {ex.Message})";
        }
    }

    // ─── Syntax highlight ─────────────────────────────────────────────────────

    private void ApplySyntaxHighlight(RichTextBox rtb)
    {
        SendMessage(rtb.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            int sel = rtb.SelectionStart;
            rtb.SelectAll();
            rtb.SelectionColor = UiTheme.SyntaxDefault;

            var text = rtb.Text;
            int pos = 0;
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.TrimStart();

                if (trimmed.StartsWith('#'))
                {
                    // Comment — gray
                    ColorSpan(rtb, pos, line.Length, UiTheme.SyntaxComment);
                }
                else if (trimmed.StartsWith('<'))
                {
                    // Directive block tag — orange
                    int tagEnd = line.IndexOf('>');
                    if (tagEnd >= 0)
                        ColorSpan(rtb, pos, tagEnd + 1, UiTheme.SyntaxSection);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    // keyword value split — keyword blue, value near-white
                    int spaceIdx = line.IndexOfAny([' ', '\t']);
                    if (spaceIdx > 0)
                    {
                        ColorSpan(rtb, pos, spaceIdx, UiTheme.SyntaxKeyword);
                        ColorSpan(rtb, pos + spaceIdx, line.Length - spaceIdx, UiTheme.SyntaxValue);
                        ColorQuotedSpans(rtb, pos, line);
                    }
                }

                pos += line.Length + 1; // +1 for the \n
            }

            rtb.SelectionStart = sel;
            rtb.SelectionLength = 0;
        }
        finally
        {
            SendMessage(rtb.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            rtb.Invalidate();
        }
    }

    /// <summary>Tints every "quoted" run on a line — in httpd.conf these are the paths.</summary>
    private static void ColorQuotedSpans(RichTextBox rtb, int lineStart, string line)
    {
        int i = 0;
        while (true)
        {
            int open = line.IndexOf('"', i);
            if (open < 0) break;
            int close = line.IndexOf('"', open + 1);
            if (close < 0) break;
            ColorSpan(rtb, lineStart + open, close - open + 1, UiTheme.SyntaxString);
            i = close + 1;
        }
    }

    private static void ColorSpan(RichTextBox rtb, int start, int length, Color color)
    {
        if (start < 0 || length <= 0 || start + length > rtb.TextLength) return;
        rtb.SelectionStart = start;
        rtb.SelectionLength = length;
        rtb.SelectionColor = color;
    }

    // ─── VHost file selection ─────────────────────────────────────────────────

    private void lstVHostFiles_SelectedIndexChanged(object sender, EventArgs e)
    {
        var name = lstVHostFiles.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(name)) return;
        var path = Path.Combine(_vhostsDir, name);
        if (!File.Exists(path))
        {
            // Stale vhost files are deleted on every Apache start; refresh rather than show nothing.
            LoadVHostFiles();
            return;
        }
        var text = TryReadAllText(path, out var error);
        if (text is null)
        {
            rtbVHostContent.Text = $"({name} could not be read: {error})";
            return;
        }
        rtbVHostContent.Text = text;
        ApplySyntaxHighlight(rtbVHostContent);
    }

    // ─── Toolbar buttons ──────────────────────────────────────────────────────

    private void btnReloadConf_Click(object sender, EventArgs e)
    {
        LoadHttpdConf();
        LoadVHostFiles();
    }

    // ─── Save / Cancel ────────────────────────────────────────────────────────

    private bool _saving;

    private async void btnApacheSave_Click(object sender, EventArgs e)
    {
        if (_saving) return;

        // Normalise line endings and trailing blanks so re-saving the same text is not a "change"
        // that bounces Apache for nothing — the old Save restarted it on every click.
        var directives = txtCustomDirectives.Text.Replace("\r\n", "\n").TrimEnd().Replace("\n", Environment.NewLine);
        var current = (_config.Apache.CustomDirectives ?? "").Replace("\r\n", "\n").TrimEnd().Replace("\n", Environment.NewLine);
        if (string.Equals(directives, current, StringComparison.Ordinal))
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (directives.Length > 0)
        {
            _saving = true;
            btnApacheSave.Enabled = false;
            UseWaitCursor = true;
            (bool Ok, string Output) test;
            try
            {
                test = await _server.TestApacheConfigAsync(directives);
            }
            finally
            {
                _saving = false;
                if (!IsDisposed)
                {
                    btnApacheSave.Enabled = true;
                    UseWaitCursor = false;
                }
            }
            if (IsDisposed) return;

            if (!test.Ok)
            {
                MessageBox.Show(this,
                    "Apache rejected the custom directives, so nothing was saved and Apache was left running as it was.\n\n" +
                    test.Output,
                    "Hamster Server — Invalid Apache directives", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCustomDirectives.Focus();
                return;
            }
        }

        _config.Apache.CustomDirectives = directives;
        ConfigChanged = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // The config test is still running; closing now would let its continuation touch a
        // disposed form.
        if (_saving) { e.Cancel = true; return; }
        base.OnFormClosing(e);
    }

    private void btnApacheCancel_Click(object sender, EventArgs e) => Close();
}
