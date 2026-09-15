using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Forms;

public partial class VHostDialog : Form
{
    // ── Result ────────────────────────────────────────────────────────────────
    public VHostEntry? Result { get; private set; }
    public bool PhpConfigChanged { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly AppConfig _config;
    private readonly string _rootDir;
    private readonly VHostEntry? _originalEntry;
    private readonly HashSet<string> _pendingExtensions;
    private HashSet<string>? _availableExtensions;
    private FrameworkProfile? _previousProfile;

    private sealed record PortItem(int Port, string Label) { public override string ToString() => Label; }

    private readonly ToolTip _tt = new() { AutoPopDelay = 6000, InitialDelay = 400 };

    public VHostDialog(AppConfig config, string rootDir, VHostEntry? existing)
    {
        InitializeComponent();
        _config = config;
        _rootDir = rootDir;
        _originalEntry = existing;
        _pendingExtensions = config.Php.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        cmbFramework.DisplayMember = nameof(FrameworkProfile.DisplayName);
        cmbFramework.DataSource = FrameworkProfiles.All.ToList();

        // Populate port combo
        PopulatePortCombo();

        // Tooltips
        _tt.SetToolTip(txtServerAlias, "Space-separated additional hostnames (e.g. www.mysite.local)");
        _tt.SetToolTip(cmbFramework, "Choose a PHP framework — adjusts Apache config and doc root suggestion");
        _tt.SetToolTip(btnBrowse, "Browse for the document root folder");

        // Action button only; Browse… / OK / Cancel stay bare like any dialog button. No AutoSize:
        // this one is anchored Left|Right so it lines up with the controls in the rows above, and
        // AutoSize would collapse it to its text width — the cell was widened instead.
        UiTheme.ApplyGlyph(btnEnableExtensions, UiTheme.Glyph.Accept, UiTheme.AccentAction, autoSize: false);

        if (existing != null)
        {
            Text = "Edit Virtual Host";
            lblTitle.Text = "Edit virtual host";
            lblSub.Text = $"Editing: {existing.ServerName}";
            FillForm(existing);
        }
        else
        {
            Text = "Add Virtual Host";
            lblTitle.Text = "Add a virtual host";
            lblSub.Text = "Configure a new Apache virtual host";
            cmbFramework.SelectedIndex = 0;
            if (cmbPort.Items.Count > 0) cmbPort.SelectedIndex = 0;
        }

        txtServerName.TextChanged += (_, _) => SetValidationError(txtServerName, false);
        txtDocRoot.TextChanged += (_, _) =>
        {
            SetValidationError(txtDocRoot, false);
            UpdateProfileSummary();
        };
        UpdateProfileSummary();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ── Port combo ────────────────────────────────────────────────────────────

    private void PopulatePortCombo()
    {
        cmbPort.Items.Clear();
        cmbPort.Items.Add(new PortItem(_config.Apache.Port, $"{_config.Apache.Port}  (Primary HTTP)"));
        if (_config.Apache.EnableSsl)
            cmbPort.Items.Add(new PortItem(_config.Apache.SslPort, $"{_config.Apache.SslPort}  (Primary HTTPS)"));
        foreach (var ep in (_config.Apache.ExtraPorts ?? []).Where(p => p.Enabled))
            cmbPort.Items.Add(new PortItem(ep.Port,
                $"{ep.Port}  ({(ep.IsSsl ? "Extra HTTPS" : "Extra HTTP")})"));
        if (cmbPort.Items.Count > 0) cmbPort.SelectedIndex = 0;
    }

    // ── Framework ─────────────────────────────────────────────────────────────

    private FrameworkProfile SelectedProfile =>
        cmbFramework.SelectedItem as FrameworkProfile ?? FrameworkProfiles.Get(FrameworkPreset.None);

    private FrameworkPreset SelectedFramework => SelectedProfile.Preset;

    private void cmbFramework_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var profile = SelectedProfile;
        bool isProxy = profile.IsReverseProxy;
        bool hasSubDir = profile.WebRootCandidates.Length > 0;

        if (_previousProfile is not null && _previousProfile.Preset != profile.Preset &&
            !_previousProfile.IsReverseProxy && !isProxy &&
            !string.IsNullOrWhiteSpace(txtDocRoot.Text))
        {
            var projectRoot = FrameworkProfiles.GetProjectRoot(txtDocRoot.Text.Trim(), _previousProfile.Preset);
            txtDocRoot.Text = FrameworkProfiles.ResolveDocumentRoot(projectRoot, profile.Preset);
        }

        lblEditorRoot.Text = isProxy ? "Proxy URL *" : "Project / web root *";
        btnBrowse.Visible = !isProxy;

        if (isProxy &&
            (!Uri.TryCreate(txtDocRoot.Text, UriKind.Absolute, out var uri) ||
             uri.Scheme is not ("http" or "https")))
        {
            txtDocRoot.Text = profile.ProxyDefault;
        }
        else if (!isProxy && Uri.TryCreate(txtDocRoot.Text, UriKind.Absolute, out var currentUri) &&
                 currentUri.Scheme is "http" or "https")
        {
            txtDocRoot.Clear();
        }

        if (isProxy)
        {
            lblDocNote.Text = "Enter the URL of the running Swoole/Hyperf server (e.g. http://127.0.0.1:9501).";
        }
        else if (hasSubDir && profile.AppendPreferredWebRoot)
        {
            lblDocNote.Text = $"Select the project root; /{profile.PreferredWebRoot} is selected automatically.";
        }
        else if (hasSubDir)
        {
            lblDocNote.Text = $"Select the project root; {string.Join(" or ", profile.WebRootCandidates.Select(p => $"/{p}"))} is auto-detected.";
        }
        else
        {
            lblDocNote.Text = "Enter the full path to the document root folder.";
        }
        lblDocNote.Visible = true;

        _tt.SetToolTip(txtDocRoot, hasSubDir
            ? "Select either the project root or its public web directory."
            : isProxy
                ? $"URL of the running {profile.DisplayName} server"
                : "Full path to the document root folder");
        _tt.SetToolTip(btnBrowse, hasSubDir
            ? "Browse to the project root; the public directory will be detected automatically"
            : "Browse for the document root folder");

        _previousProfile = profile;
        UpdateProfileSummary();
    }

    // ── Browse ────────────────────────────────────────────────────────────────

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select project root folder",
            SelectedPath = Path.Combine(_rootDir, "www"),
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        txtDocRoot.Text = FrameworkProfiles.ResolveDocumentRoot(dlg.SelectedPath, SelectedFramework);
    }

    private void UpdateProfileSummary()
    {
        if (lblProfileDescription is null) return;

        var profile = SelectedProfile;
        btnEnableExtensions.Visible = false;
        lblProfileDescription.Text = profile.Description;

        var webRoot = profile.IsReverseProxy ? "Reverse proxy"
                    : string.IsNullOrEmpty(profile.PreferredWebRoot) ? "Selected directory"
                    : string.Join(" / ", profile.WebRootCandidates);
        var routing = profile.IsReverseProxy ? "every request proxied upstream"
                    : profile.UsesFrontController ? "index.php front controller"
                    : "Direct file handling";
        lblProfileRules.Text = $"Web root: {webRoot}    Routing: {routing}";

        var missing = FrameworkProfiles.GetMissingManagedExtensions(
            profile, _pendingExtensions);
        if (missing.Length > 0)
        {
            var version = _config.Php.CurrentVersion;
            var available = GetAvailableExtensions();
            var unavailable = missing.Where(extension => !available.Contains(extension)).ToArray();
            var enableable = missing.Where(available.Contains).ToArray();
            var messages = new List<string>();
            if (enableable.Length > 0)
                messages.Add($"Not enabled: {string.Join(", ", enableable)}");
            if (unavailable.Length > 0)
                messages.Add($"Not bundled with PHP {version}: {string.Join(", ", unavailable)}");
            lblProfileStatus.Text = string.Join(".  ", messages);
            lblProfileStatus.ForeColor = UiTheme.StatusTransition;
            btnEnableExtensions.Visible = enableable.Length > 0;
            btnEnableExtensions.Tag = enableable;
            return;
        }

        var value = txtDocRoot.Text.Trim();
        if (profile.IsReverseProxy)
        {
            lblProfileStatus.Text = "Profile requirements satisfied; the upstream server must be started separately.";
            lblProfileStatus.ForeColor = UiTheme.AccentAction;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            lblProfileStatus.Text = profile.RequiredExtensions.Length == 0
                ? "Choose a directory to validate the project."
                : "Required PHP extensions are enabled. Choose a directory to validate the project.";
            lblProfileStatus.ForeColor = SystemColors.GrayText;
            return;
        }

        var projectRoot = FrameworkProfiles.GetProjectRoot(value, profile.Preset);
        if (!Directory.Exists(projectRoot))
        {
            lblProfileStatus.Text = "Directory does not exist yet; the profile will be applied when the project is created.";
            lblProfileStatus.ForeColor = UiTheme.StatusTransition;
            return;
        }

        bool recognized = profile.ProjectMarkers.Length == 0 || profile.ProjectMarkers.Any(marker =>
        {
            var candidate = Path.Combine(projectRoot, marker.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(candidate) || Directory.Exists(candidate);
        });
        lblProfileStatus.Text = recognized
            ? "Project structure recognized and PHP extension requirements are satisfied."
            : $"Directory exists, but {profile.DisplayName} project markers were not found.";
        lblProfileStatus.ForeColor = recognized ? UiTheme.StatusRunning : UiTheme.StatusTransition;
    }

    private HashSet<string> GetAvailableExtensions()
    {
        if (_availableExtensions is not null) return _availableExtensions;

        var extensionDir = Path.Combine(_rootDir, "php", _config.Php.CurrentVersion, "ext");
        try
        {
            _availableExtensions = Directory.Exists(extensionDir)
                ? Directory.EnumerateFiles(extensionDir, "php_*.dll")
                    .Select(path => Path.GetFileNameWithoutExtension(path)[4..])
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];
        }
        catch (IOException)
        {
            _availableExtensions = [];
        }
        catch (UnauthorizedAccessException)
        {
            _availableExtensions = [];
        }

        return _availableExtensions;
    }

    private void btnEnableExtensions_Click(object sender, EventArgs e)
    {
        if (btnEnableExtensions.Tag is not string[] extensions) return;
        _pendingExtensions.UnionWith(extensions);
        UpdateProfileSummary();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static void SetValidationError(Control ctrl, bool hasError)
        => ctrl.BackColor = hasError ? UiTheme.FieldError : UiTheme.FieldNormal;

    // ── Save ──────────────────────────────────────────────────────────────────

    private void btnSave_Click(object sender, EventArgs e)
    {
        var name = txtServerName.Text.Trim();
        if (!ApacheConfigWriter.IsValidServerName(name))
        {
            SetValidationError(txtServerName, true);
            MessageBox.Show("Server Name must be a valid DNS host name without spaces.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtServerName.Focus();
            return;
        }

        var aliases = txtServerAlias.Text.Trim();
        if (!ApacheConfigWriter.AreValidServerAliases(aliases))
        {
            MessageBox.Show("Each Server Alias must be a valid DNS host name. Separate aliases with spaces.",
                "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtServerAlias.Focus();
            return;
        }

        var duplicate = _config.VHosts.FirstOrDefault(
            v => v.ServerName.Equals(name, StringComparison.OrdinalIgnoreCase)
                 && v != _originalEntry);
        if (duplicate != null)
        {
            MessageBox.Show("A VHost with that Server Name already exists.", "Duplicate",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var docRoot = txtDocRoot.Text.Trim();

        // Auto-resolve subdir for framework presets even when path is typed manually
        if (!string.IsNullOrWhiteSpace(docRoot) && !SelectedProfile.IsReverseProxy)
        {
            docRoot = FrameworkProfiles.ResolveDocumentRoot(docRoot, SelectedFramework);
            txtDocRoot.Text = docRoot;
        }

        if (string.IsNullOrWhiteSpace(docRoot))
        {
            SetValidationError(txtDocRoot, true);
            var msg = SelectedProfile.IsReverseProxy
                ? "Proxy URL is required (e.g. http://127.0.0.1:9501)."
                : "Document Root is required.";
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDocRoot.Focus();
            return;
        }

        if (SelectedProfile.IsReverseProxy &&
            (!Uri.TryCreate(docRoot, UriKind.Absolute, out var proxyUri) ||
             proxyUri.Scheme is not ("http" or "https")))
        {
            SetValidationError(txtDocRoot, true);
            MessageBox.Show("Proxy URL must start with http:// or https://",
                "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDocRoot.Focus();
            return;
        }

        var originalExtensions = _config.Php.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!originalExtensions.SetEquals(_pendingExtensions))
        {
            _config.Php.EnabledExtensions = _pendingExtensions
                .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                .ToList();
            PhpConfigChanged = true;
        }

        var entry = _originalEntry ?? new VHostEntry();
        entry.ServerName = name;
        entry.ServerAlias = aliases;
        entry.DocumentRoot = docRoot;
        entry.Port = cmbPort.SelectedItem is PortItem pi ? pi.Port : _config.Apache.Port;
        entry.Framework = SelectedFramework;
        entry.Enabled = chkEnabled.Checked;

        Result = entry;
        DialogResult = DialogResult.OK;
    }

    private void btnCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;

    // ── Fill ──────────────────────────────────────────────────────────────────

    private void FillForm(VHostEntry v)
    {
        txtServerName.Text = v.ServerName;
        txtServerAlias.Text = v.ServerAlias;
        chkEnabled.Checked = v.Enabled;

        var fwkIdx = FrameworkProfiles.All.ToList().FindIndex(profile => profile.Preset == v.Framework);
        cmbFramework.SelectedIndex = fwkIdx >= 0 ? fwkIdx : 0;

        txtDocRoot.Text = v.DocumentRoot;

        var portToFind = v.Port > 0 ? v.Port : _config.Apache.Port;
        var idx = Enumerable.Range(0, cmbPort.Items.Count)
            .FirstOrDefault(i => cmbPort.Items[i] is PortItem pi && pi.Port == portToFind, 0);
        cmbPort.SelectedIndex = idx;
    }
}
