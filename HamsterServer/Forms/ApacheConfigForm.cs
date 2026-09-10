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

    private void LoadHttpdConf()
    {
        if (!File.Exists(_httpdConfPath))
        {
            rtbHttpdConf.Text = "(httpd.conf has not been generated yet — start Apache first)";
            return;
        }
        rtbHttpdConf.Text = File.ReadAllText(_httpdConfPath);
        ApplySyntaxHighlight(rtbHttpdConf);
    }

    private void LoadVHostFiles()
    {
        lstVHostFiles.Items.Clear();
        if (!Directory.Exists(_vhostsDir)) return;
        foreach (var f in Directory.GetFiles(_vhostsDir, "*.conf").OrderBy(f => f))
            lstVHostFiles.Items.Add(Path.GetFileName(f));
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
        if (!File.Exists(path)) return;
        rtbVHostContent.Text = File.ReadAllText(path);
        ApplySyntaxHighlight(rtbVHostContent);
    }

    // ─── Toolbar buttons ──────────────────────────────────────────────────────

    private void btnReloadConf_Click(object sender, EventArgs e) => LoadHttpdConf();

    // ─── Save / Cancel ────────────────────────────────────────────────────────

    private void btnApacheSave_Click(object sender, EventArgs e)
    {
        _config.Apache.CustomDirectives = txtCustomDirectives.Text;
        ConfigChanged = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnApacheCancel_Click(object sender, EventArgs e) => Close();
}
