using HamsterServer.Models;

namespace HamsterServer.Core;

public enum PhpUpdateState
{
    /// <summary>Never checked, or updates are off.</summary>
    Idle,
    Checking,
    UpToDate,
    /// <summary>A newer patch exists (Notify mode, or Auto before it starts installing).</summary>
    UpdateAvailable,
    Installing,
    /// <summary>Installed; waiting for a moment with no request in flight to switch to it.</summary>
    WaitingToSwitch,
    /// <summary>Switched to the new patch during this session.</summary>
    Updated,
    Failed
}

public sealed class PhpUpdatedEventArgs(string fromVersion, string toVersion, IReadOnlyList<string> copiedFiles, bool switched) : EventArgs
{
    public string FromVersion { get; } = fromVersion;
    public string ToVersion { get; } = toVersion;
    /// <summary>Extension and dependency DLLs carried over from the previous runtime.</summary>
    public IReadOnlyList<string> CopiedFiles { get; } = copiedFiles;
    /// <summary>False when the new runtime was installed but is not the active one (yet).</summary>
    public bool Switched { get; } = switched;
}

/// <summary>
/// Keeps the active PHP runtime on the latest patch of its branch.
/// <para>
/// A patch update is always installed side-by-side — the previous runtime stays for rollback — and
/// the extension DLLs the user added to the old runtime are copied into the new one
/// (<see cref="PhpVersionManager.CopyMissingFiles"/>), so an update never silently drops sqlsrv or
/// xdebug. php.ini needs nothing copied: it is regenerated from AppConfig for every runtime.
/// Moving to another major.minor is never automatic: extension DLLs are built per minor branch.
/// </para>
/// <para>
/// Not thread-safe by design: the main window calls it from the UI thread, so every await resumes
/// there and its state is only ever touched by one thread — the same thread the manager window and
/// the PHP version combo use. <see cref="PhpVersionManager.InstallAsync"/> serialises installs.
/// </para>
/// </summary>
public sealed class PhpUpdateService(PhpVersionManager versions, ServerManager server)
{
    private string? _announcedVersion;

    public PhpUpdateState State { get; private set; } = PhpUpdateState.Idle;

    /// <summary>The newer patch of the active branch, when one exists.</summary>
    public PhpReleaseInfo? AvailableUpdate { get; private set; }

    public DateTimeOffset? LastChecked { get; private set; }
    public string? LastError { get; private set; }
    public bool IsBusy => State is PhpUpdateState.Checking or PhpUpdateState.Installing or PhpUpdateState.WaitingToSwitch;

    /// <summary>How often to re-check for a gap between requests before switching (tests shorten it).</summary>
    public int IdlePollMs { get; init; } = 15_000;

    public event EventHandler? StateChanged;
    public event EventHandler<string>? LogReceived;
    public event EventHandler<PhpUpdatedEventArgs>? Updated;

    /// <summary>
    /// One check, plus — in <see cref="PhpUpdateMode.Auto"/> — install and switch. Safe to call on a
    /// timer: a call while a previous one is still running returns immediately.
    /// </summary>
    public async Task CheckAsync(PhpUpdateMode mode, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        if (mode == PhpUpdateMode.Off)
        {
            AvailableUpdate = null;
            SetState(PhpUpdateState.Idle);
            return;
        }

        string current = server.Config.Php.CurrentVersion;
        SetState(PhpUpdateState.Checking);
        IReadOnlyList<PhpReleaseInfo> releases;
        try
        {
            releases = await versions.GetAvailableReleasesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            SetState(PhpUpdateState.Idle);
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"[WARN] PHP update check failed: {ex.Message}");
            SetState(PhpUpdateState.Failed);
            return;
        }

        LastChecked = DateTimeOffset.Now;
        LastError = null;
        AvailableUpdate = versions.GetUpdate(current, releases);
        if (AvailableUpdate is null)
        {
            SetState(PhpUpdateState.UpToDate);
            return;
        }

        if (mode == PhpUpdateMode.Notify)
        {
            if (!string.Equals(_announcedVersion, AvailableUpdate.Version, StringComparison.OrdinalIgnoreCase))
            {
                _announcedVersion = AvailableUpdate.Version;
                Log($"PHP {AvailableUpdate.Version} is available (current {current}). Open Tools → PHP Version Manager to update.");
            }
            SetState(PhpUpdateState.UpdateAvailable);
            return;
        }

        await ApplyUpdateAsync(current, AvailableUpdate, progress: null, switchImmediately: false, cancellationToken);
    }

    /// <summary>
    /// Installs <paramref name="update"/> next to <paramref name="fromVersion"/> (unless it already is),
    /// copies the extension files the old runtime has and the new one lacks, regenerates php.ini, and —
    /// when <paramref name="fromVersion"/> is the active runtime — switches to the new one.
    /// </summary>
    /// <param name="switchImmediately">
    /// True for a click in the manager window: switch now. False for the background updater: wait for
    /// a moment when Apache is serving nothing, as the memory watchdog does, so no request is cut.
    /// </param>
    /// <returns>False when the update failed (the reason is logged and in <see cref="LastError"/>).</returns>
    public async Task<bool> ApplyUpdateAsync(
        string fromVersion,
        PhpReleaseInfo update,
        IProgress<PhpDownloadProgress>? progress,
        bool switchImmediately,
        CancellationToken cancellationToken = default)
    {
        if (!PhpVersionManager.IsSameBranch(fromVersion, update.Version))
            throw new InvalidOperationException(
                $"PHP {update.Version} is not a patch of {fromVersion}; switching branches is a manual choice.");

        // State describes the active runtime; updating another one must hand the old state back.
        var stateBefore = State;
        bool aboutActive = string.Equals(server.Config.Php.CurrentVersion, fromVersion, StringComparison.OrdinalIgnoreCase);
        try
        {
            versions.Scan();
            if (versions.GetVersion(update.Version) is null)
            {
                SetState(PhpUpdateState.Installing);
                Log($"Installing PHP {update.Version} next to {fromVersion}…");
                await versions.InstallAsync(update, progress, cancellationToken);
            }

            var copied = versions.CopyMissingFiles(update.Version, fromVersion);
            if (copied.Count > 0)
                Log($"Carried over from PHP {fromVersion}: {string.Join(", ", copied.Select(Path.GetFileName))}");
            server.SynchronizePhpIni();

            bool switched = false;
            if (string.Equals(server.Config.Php.CurrentVersion, fromVersion, StringComparison.OrdinalIgnoreCase))
            {
                if (!switchImmediately && server.IsPhpRunning)
                {
                    SetState(PhpUpdateState.WaitingToSwitch);
                    Log($"PHP {update.Version} is ready; switching at the first moment no request is in flight.");
                    while (!await server.IsPhpPoolIdleAsync())
                        await Task.Delay(IdlePollMs, cancellationToken);
                }

                // The user may have picked another version while this waited; theirs wins.
                if (string.Equals(server.Config.Php.CurrentVersion, fromVersion, StringComparison.OrdinalIgnoreCase))
                {
                    await server.SwitchPhpVersionAsync(update.Version);
                    switched = true;
                    Log($"PHP updated {fromVersion} → {update.Version}. {fromVersion} is kept for rollback.");
                }
            }

            if (switched && AvailableUpdate?.Version == update.Version) AvailableUpdate = null;
            SetState(switched ? PhpUpdateState.Updated : aboutActive ? PhpUpdateState.UpdateAvailable : stateBefore);
            Updated?.Invoke(this, new PhpUpdatedEventArgs(fromVersion, update.Version, copied, switched));
            return true;
        }
        catch (OperationCanceledException)
        {
            SetState(PhpUpdateState.Idle);
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"[ERROR] PHP update to {update.Version} failed: {ex.Message}");
            SetState(aboutActive ? PhpUpdateState.Failed : stateBefore);
            return false;
        }
    }

    private void SetState(PhpUpdateState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Log(string message) =>
        LogReceived?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] [PHP] {message}");
}
