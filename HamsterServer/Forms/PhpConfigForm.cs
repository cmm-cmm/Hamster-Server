using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using HamsterServer.Core;

namespace HamsterServer.Forms;

public partial class PhpConfigForm : Form
{
    private readonly PhpVersionInfo _phpInfo;
    private readonly ServerManager _server;
    private readonly string _phpIniPath;
    private readonly string _extDir;

    private HashSet<string> _enabledExts = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _availableExts = [];

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_SETREDRAW = 0x000B;

    public PhpConfigForm(PhpVersionInfo phpInfo, ServerManager server)
    {
        InitializeComponent();
        _phpInfo = phpInfo;
        _server = server;
        _phpIniPath = Path.Combine(phpInfo.DirectoryPath, "php.ini");
        _extDir = Path.Combine(phpInfo.DirectoryPath, "ext");

        Text = $"PHP {phpInfo.Version} Configuration";
        lblPhpTitle.Text = $"PHP {phpInfo.Version} — {_phpIniPath}";
        rtbPhpIni.Font = UiTheme.CreateMonospaceFont();

        // Only the action button gets a glyph — Save and Close are plain dialog buttons.
        // Amber, because this one bounces a running service.
        UiTheme.ApplyGlyph(btnPhpSaveRestart, UiTheme.Glyph.Refresh, UiTheme.StatusTransition);

        LoadFromIni();
        LoadExtensions();
        LoadRawIni();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    private void LoadFromIni()
    {
        if (!File.Exists(_phpIniPath)) return;
        var lines = File.ReadAllLines(_phpIniPath);
        var kvRx = new Regex(@"^\s*(?<key>[a-zA-Z_.]+)\s*=\s*(?<val>.+?)\s*$");
        var extRx = new Regex(@"^\s*extension\s*=\s*(?<name>[a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);

        _enabledExts.Clear();
        foreach (var line in lines)
        {
            var m = extRx.Match(line);
            if (m.Success && !line.TrimStart().StartsWith(';'))
            {
                _enabledExts.Add(m.Groups["name"].Value);
                continue;
            }

            var kv = kvRx.Match(line);
            if (!kv.Success || line.TrimStart().StartsWith(';')) continue;

            var key = kv.Groups["key"].Value;
            var val = kv.Groups["val"].Value.Trim('"');

            switch (key.ToLowerInvariant())
            {
                case "memory_limit":
                    if (int.TryParse(val.TrimEnd('m', 'M', 'g', 'G'), out int mem))
                        nudMemoryLimit.Value = Math.Clamp(mem, (int)nudMemoryLimit.Minimum, (int)nudMemoryLimit.Maximum);
                    break;
                case "upload_max_filesize":
                    if (int.TryParse(val.TrimEnd('m', 'M'), out int up))
                        nudUploadMax.Value = Math.Clamp(up, (int)nudUploadMax.Minimum, (int)nudUploadMax.Maximum);
                    break;
                case "post_max_size":
                    if (int.TryParse(val.TrimEnd('m', 'M'), out int pm))
                        nudPostMax.Value = Math.Clamp(pm, (int)nudPostMax.Minimum, (int)nudPostMax.Maximum);
                    break;
                case "max_execution_time":
                    if (int.TryParse(val, out int ex))
                        nudMaxExecTime.Value = Math.Clamp(ex, (int)nudMaxExecTime.Minimum, (int)nudMaxExecTime.Maximum);
                    break;
                case "display_errors":
                    chkDisplayErrors.Checked = val.Equals("On", StringComparison.OrdinalIgnoreCase) || val == "1";
                    break;
                case "short_open_tag":
                    chkShortOpenTag.Checked = val.Equals("On", StringComparison.OrdinalIgnoreCase) || val == "1";
                    break;
                case "error_reporting":
                    cmbErrorReporting.SelectedItem = val switch
                    {
                        "E_ALL" => "E_ALL (all errors)",
                        "E_ALL & ~E_DEPRECATED & ~E_STRICT" => "E_ALL except deprecated",
                        "E_ALL & ~E_NOTICE" => "E_ALL except notices",
                        _ => "Custom"
                    };
                    if (cmbErrorReporting.SelectedIndex < 0) cmbErrorReporting.SelectedIndex = 0;
                    break;
                case "date.timezone":
                    txtTimezone.Text = val;
                    break;
            }
        }
    }

    private void LoadExtensions()
    {
        _availableExts = [];
        if (Directory.Exists(_extDir))
        {
            _availableExts = Directory.GetFiles(_extDir, "*.dll")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => n.StartsWith("php_", StringComparison.OrdinalIgnoreCase))
                .Select(n => n[4..]) // strip "php_" prefix
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        clbExtensions.BeginUpdate();
        try
        {
            clbExtensions.Items.Clear();
            foreach (var ext in _availableExts)
                clbExtensions.Items.Add(ext, _enabledExts.Contains(ext));
        }
        finally { clbExtensions.EndUpdate(); }
    }

    private void LoadRawIni()
    {
        if (!File.Exists(_phpIniPath))
        {
            rtbPhpIni.Text = "(php.ini not found — start PHP once to generate it)";
            return;
        }
        rtbPhpIni.Text = File.ReadAllText(_phpIniPath);
        ApplySyntaxHighlight(rtbPhpIni);
    }

    // ─── Syntax highlight ─────────────────────────────────────────────────────

    private void ApplySyntaxHighlight(RichTextBox rtb)
    {
        SendMessage(rtb.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            int savedSel = rtb.SelectionStart;
            rtb.SelectAll();
            rtb.SelectionColor = UiTheme.SyntaxDefault;

            var text = rtb.Text;
            int pos = 0;
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(';'))
                {
                    ColorSpan(rtb, pos, line.Length, UiTheme.SyntaxComment);
                }
                else if (trimmed.StartsWith('['))
                {
                    ColorSpan(rtb, pos, line.Length, UiTheme.SyntaxSection);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    int eqIdx = line.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        ColorSpan(rtb, pos, eqIdx, UiTheme.SyntaxKeyword);
                        ColorSpan(rtb, pos + eqIdx, line.Length - eqIdx, UiTheme.SyntaxValue);

                        // Quoted paths, then bare On/Off and numeric values, get their own colour
                        var value = line[(eqIdx + 1)..];
                        int valueStart = pos + eqIdx + 1;
                        if (value.Contains('"'))
                        {
                            int open = value.IndexOf('"');
                            int close = value.LastIndexOf('"');
                            if (close > open)
                                ColorSpan(rtb, valueStart + open, close - open + 1, UiTheme.SyntaxString);
                        }
                        else
                        {
                            var literal = value.Trim();
                            if (literal.Length > 0 &&
                                (char.IsDigit(literal[0]) ||
                                 literal.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                                 literal.Equals("Off", StringComparison.OrdinalIgnoreCase)))
                            {
                                int offset = value.IndexOf(literal[0]);
                                ColorSpan(rtb, valueStart + offset, literal.Length, UiTheme.SyntaxNumber);
                            }
                        }
                    }
                }
                pos += line.Length + 1;
            }

            rtb.SelectionStart = savedSel;
            rtb.SelectionLength = 0;
        }
        finally
        {
            SendMessage(rtb.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            rtb.Invalidate();
        }
    }

    private static void ColorSpan(RichTextBox rtb, int start, int length, Color color)
    {
        if (start < 0 || length <= 0 || start + length > rtb.TextLength) return;
        rtb.SelectionStart = start;
        rtb.SelectionLength = length;
        rtb.SelectionColor = color;
    }

    // ─── Extension filter ─────────────────────────────────────────────────────

    private void txtExtFilter_TextChanged(object sender, EventArgs e)
    {
        var filter = txtExtFilter.Text.Trim();
        clbExtensions.BeginUpdate();
        try
        {
            clbExtensions.Items.Clear();
            foreach (var ext in _availableExts)
            {
                if (string.IsNullOrWhiteSpace(filter) || ext.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    clbExtensions.Items.Add(ext, _enabledExts.Contains(ext));
            }
        }
        finally { clbExtensions.EndUpdate(); }
    }

    private void clbExtensions_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        var ext = clbExtensions.Items[e.Index]?.ToString() ?? "";
        if (e.NewValue == CheckState.Checked)
            _enabledExts.Add(ext);
        else
            _enabledExts.Remove(ext);
    }

    // ─── Reload raw ───────────────────────────────────────────────────────────

    private void btnReloadIni_Click(object sender, EventArgs e) => LoadRawIni();

    // ─── Save ─────────────────────────────────────────────────────────────────

    private async void btnPhpSaveRestart_Click(object sender, EventArgs e)
    {
        SaveToIni();
        if (_server.IsPhpRunning)
        {
            btnPhpSaveRestart.Enabled = false;
            try
            {
                await _server.StopPhpAsync();
                await _server.StartPhpAsync();
            }
            finally
            {
                if (!IsDisposed) btnPhpSaveRestart.Enabled = true;
            }
        }
        if (IsDisposed) return;   // the dialog may have been closed during the restart
        LoadRawIni();
    }

    private void btnPhpSave_Click(object sender, EventArgs e)
    {
        SaveToIni();
        LoadRawIni();
    }

    private void btnPhpCancel_Click(object sender, EventArgs e) => Close();

    private void SaveToIni()
    {
        if (!File.Exists(_phpIniPath))
        {
            MessageBox.Show("php.ini not found. Start PHP once to generate it.", "Hamster Server",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var content = File.ReadAllText(_phpIniPath);

        content = SetIniValue(content, "memory_limit", $"{(int)nudMemoryLimit.Value}M");
        content = SetIniValue(content, "upload_max_filesize", $"{(int)nudUploadMax.Value}M");
        content = SetIniValue(content, "post_max_size", $"{(int)nudPostMax.Value}M");
        content = SetIniValue(content, "max_execution_time", $"{(int)nudMaxExecTime.Value}");
        content = SetIniValue(content, "max_input_time", $"{(int)nudMaxExecTime.Value}");
        content = SetIniValue(content, "display_errors", chkDisplayErrors.Checked ? "On" : "Off");
        content = SetIniValue(content, "short_open_tag", chkShortOpenTag.Checked ? "On" : "Off");
        content = SetIniValue(content, "date.timezone", txtTimezone.Text.Trim());

        var errReporting = cmbErrorReporting.SelectedItem?.ToString() switch
        {
            "E_ALL (all errors)" => "E_ALL",
            "E_ALL except deprecated" => "E_ALL & ~E_DEPRECATED & ~E_STRICT",
            "E_ALL except notices" => "E_ALL & ~E_NOTICE",
            _ => "E_ALL"
        };
        content = SetIniValue(content, "error_reporting", errReporting);

        // Remove all existing extension= lines (active and commented)
        content = Regex.Replace(content, @"^\s*;?\s*extension\s*=.*$\r?\n?", "",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        // Append managed extension block
        var sb = new StringBuilder(content.TrimEnd());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("; Extensions managed by Hamster Server");
        foreach (var ext in _availableExts.OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
        {
            if (_enabledExts.Contains(ext))
                sb.AppendLine($"extension = {ext}");
        }

        File.WriteAllText(_phpIniPath, sb.ToString(), Encoding.UTF8);
    }

    private static string SetIniValue(string content, string key, string value)
    {
        var rx = new Regex($@"^(\s*){Regex.Escape(key)}\s*=.*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var newLine = $"{key.PadRight(25)}= {value}";
        if (rx.IsMatch(content))
            return rx.Replace(content, newLine);
        return content.TrimEnd() + Environment.NewLine + newLine + Environment.NewLine;
    }
}
