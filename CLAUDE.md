# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hamster Server is a Windows-only WinForms desktop app (.NET 10) that bundles Apache 2.4 + four PHP builds (8.2/8.3/8.4/8.5) into a local dev server. The GUI starts/stops the stack, switches PHP versions, manages virtual hosts and ports, and edits Apache/PHP settings so the user never touches config files by hand.

`AGENTS.md` holds the commit/PR conventions and coding-style rules; this file covers the parts you can only learn by reading several files at once.

## Build, Test, Run

Everything runs from the repo root on Windows. `global.json` pins SDK `10.0.400` (rollForward: latestPatch).

```powershell
dotnet restore HamsterServer.slnx
dotnet build   HamsterServer.slnx
dotnet test    HamsterServer.slnx
dotnet run     --project HamsterServer/HamsterServer.csproj
dotnet publish HamsterServer/HamsterServer.csproj -c Release
dotnet format  HamsterServer.slnx --verify-no-changes   # must produce no diff
```

The project is hosted on **GitLab**, and there is currently no pipeline definition in the repository (no `.gitlab-ci.yml`). Run the four commands above — restore, format check, test, publish — before pushing; nothing else enforces them. Any pipeline added later needs Git LFS enabled on checkout, or every runtime test fails against pointer files.

Running a subset of tests:

```powershell
dotnet test HamsterServer.slnx --filter "TestCategory!=Integration"      # skip runtime smoke tests
dotnet test HamsterServer.slnx --filter "FullyQualifiedName~ConfigManagerTests"
dotnet test HamsterServer.slnx --filter "Name=Constructor_PopulatesExtensionsBeforeWindowHandleExists"
```

**Git LFS is required.** Apache and PHP binaries (`*.dll`, `*.exe`, `*.lib`, `*.so`, `*.phar` under `HamsterServer/apache/` and `HamsterServer/php/`) are committed through LFS — see `.gitattributes`. Cloning without LFS leaves pointer files and every runtime test fails.

## Repository Layout

```
HamsterServer.slnx                 solution (Hamster Server + HamsterServer.Tests)
HamsterServer/                     the WinForms app
  Core/                      process + config logic (no UI)
  Models/                    config objects, enums, and framework profile catalog
  Forms/                     secondary dialogs
  HamsterServerForm.cs             main window
  apache/Apache24/           bundled Apache (LFS)
  php/8.2.33 … 8.5.9/        bundled PHP builds (LFS), dir name == version
  config/                    runtime config; generated files are gitignored
  www/                       default document root
HamsterServer.Tests/               MSTest
THIRD_PARTY_RUNTIMES.md      pinned runtime archives + SHA-256
```

`apache/`, `php/`, `config/`, `www/` are `Content` items copied `PreserveNewest` into the build output, so **the running app operates on `HamsterServer/bin/<cfg>/net10.0-windows/`, not on the source tree**. Generated `httpd.conf`, `vhosts/*.conf`, `php.ini`, and logs appear under the output directory. Editing source `config/` only affects the next build.

## Root Directory Resolution

`HamsterServerForm.ResolveRootDir()` does **not** look for `HamsterServer.csproj`. It accepts `AppContext.BaseDirectory` if it contains a `php/` directory plus one of `apache/`, `Apache/`, `Apache24/`; otherwise it walks up at most 5 parents looking for the same shape, and falls back to `BaseDirectory`. Everything else (`ConfigManager`, `PhpVersionManager`, `ServerManager`, `LogWriter`) is constructed with that one `rootDir` string and derives all paths from it.

## Core (`HamsterServer/Core`)

| Class | Responsibility |
|---|---|
| `ServerManager.cs` | Owns the `httpd.exe` and `php-cgi.exe` processes: start/stop/restart, health, watchdog. Raises `LogReceived` / `StatusChanged`. |
| `ApacheConfigWriter.cs` | Generates `httpd.conf`, `config/vhosts/*.conf`, and `php.ini` from `AppConfig`. |
| `ConfigManager.cs` | Loads/saves `config/hamster-server.json` atomically, normalizes it, and owns the "Start with Windows" registry value. |
| `PhpVersionManager.cs` | Scans `php/*/php-cgi.exe`, sorts by parsed version descending. |
| `LogWriter.cs` | Daily log file `config/logs/server/hamster-server-yyyy-MM-dd.log`, prunes files older than 30 days on date rollover. |

### Startup sequence

`StartAllAsync` launches PHP and Apache **concurrently** — Apache connects to FastCGI per-request, so there is no ordering dependency. `StopAllAsync` is ordered (Apache first, 300 ms, then PHP).

`StartApacheCoreAsync` does, in order: locate Apache dir → kill orphaned `httpd.exe` **belonging to this installation only** (matched by full exe path) → pre-flight check every configured port and abort with the owning process name on conflict → regenerate `httpd.conf` + vhosts → `httpd.exe -t -f <conf>` validation → spawn. The pre-flight reads the OS listener table once (`GetActiveTcpListeners`) instead of probing each port with a TCP connect; `FindPortOwner` shells out to `netstat` only for ports that actually conflict.

`StartPhpCoreAsync` writes `php.ini` (only if missing), kills matching orphaned `php-cgi`, warns when `Workers × MemoryLimit > 70%` of available RAM, then spawns `php-cgi.exe -b 127.0.0.1:<FcgiPort>` with `PHP_FCGI_CHILDREN`, `PHP_FCGI_MAX_REQUESTS` and `PHPRC` in the environment. `PHP_FCGI_MAX_CONNECTIONS` is deliberately never set (unstable on Windows).

Each worker is a process costing ~16 MB private / ~35 MB working set, so the default worker count is `Math.Clamp(ProcessorCount, 4, 8)` rather than a multiple of the core count: measured on a 12-core box, 24 workers idled at 891 MB of working set and served *fewer* requests per second at 16-way concurrency than 8 workers at 322 MB. Existing `hamster-server.json` files keep whatever they already store.

### Concurrency and lifecycle invariants

- `_apacheGate` / `_phpGate` (`SemaphoreSlim(1,1)`) serialize start/stop; public methods take the gate, `*CoreAsync` methods assume it is held.
- `_apacheStopRequested` / `_phpStopRequested` tell the exit handlers a stop was intentional, so the watchdog does not restart.
- PHP watchdog: on unexpected exit, auto-restart after 2 s, capped at 5 restarts per 120 s window; `_phpDesiredRunning` gates it and `SchedulePhpRestart` cancels any superseded attempt.
- `Dispose()` is synchronous (no `await`): kills tracked process trees, then sweeps `Process.GetProcessesByName("httpd"/"php-cgi")` and kills only those whose image path matches this installation — mpm_winnt child processes and PHP workers routinely outlive their parents.
- That path comparison goes through `TryGetProcessImagePath` (`QueryFullProcessImageName`), **not** `Process.MainModule`. `MainModule` enumerates every loaded module of the target, which made stopping PHP take ~19 s with 24 workers. Every `Process` handed back by `GetProcessesByName` is disposed, matching or not.

## Generated Configuration — hard constraints

These rules are load-bearing; violating them produces crashes or `ERR_INVALID_RESPONSE` rather than clean errors.

- **One port per `<VirtualHost>`.** `<VirtualHost *:80 *:8080>` crashes Apache instantly on mpm_winnt. `WriteAllVHosts` therefore emits one file per port: `00-default.conf` (main port), `20-port-<port>.conf` / `20-port-ssl-<port>.conf` (extra ports, skipped when a custom vhost already claims that port), `10-<slug>-<sha256[0..10]>.conf` (custom vhosts). Files not in the keep-set are deleted, so stale vhosts self-clean.
- `Models/FrameworkProfile.cs` is the single source of truth for framework labels, public web-root discovery, project markers, PHP extension requirements, front-controller behavior, and sensitive-file patterns. Both `VHostDialog` and `ApacheConfigWriter` consume it; never add a preset independently to either class. Append new `FrameworkPreset` numeric values instead of reordering existing ones, because the JSON enum converter also accepts legacy numeric values.
- **`display_errors` must always be `Off`.** Any PHP output before headers corrupts the FastCGI response. Errors go to `config/logs/php_error.log`; `output_buffering = 4096` is set for the same reason.
- **FastCGI is wired per-VirtualHost, not globally**, so `DOCUMENT_ROOT` resolves correctly. Each block sets `ProxyFCGIBackendType GENERIC`, a `ProxyPassMatch` to `fcgi://127.0.0.1:<FcgiPort>/$1`, plus a `RewriteRule` that stashes the real filesystem path in `PHP_FCGI_SCRIPT` and a `ProxyFCGISetEnvIf` that feeds it back as `SCRIPT_FILENAME` — this is what makes document roots containing spaces work.
- **`ProxyPassMatch` carries `enablereuse=on max=<Workers> ttl=15` (`ApacheConfigWriter.FcgiReuse`), and all three parts are load-bearing.** `mod_proxy_fcgi` defaults to no reuse, opening and discarding a TCP connection to `php-cgi` for *every* PHP request: latency 0.593 vs 0.287 ms, and a 20 000-request / 64-way soak ran at 1 023 vs 5 333 req/s. The mechanism shows in the socket table — 16 099 sockets left in `TIME_WAIT` toward the FastCGI port after 20 s of load, against a default Windows ephemeral range of 16 384. But `enablereuse=on` **alone is worse than no reuse at all**: `max` defaults to `ThreadsPerChild` (256), so the pool fills with connections no php-cgi child is serving and requests die on `AH01075 ... (polling)` timeouts — at 32-way concurrency throughput fell from 13 066 to **13 req/s with 10 failed requests**. Pinning `max` to the worker count restores it (~18 000 req/s, zero failures at 8–128 way), which is why `max` must track `Php.Workers` and not be hard-coded. When testing this, restart `php-cgi` between variants: a run that poisoned the backend makes the next variant look broken.
- `opcache.jit = function` (tracing mode can crash on mpm_winnt).
- `WriteIfChanged` compares content with normalized line endings before writing, so a restart does not touch unchanged files.
- **`php.ini` is only regenerated when it does not exist**, or via `ServerManager.InvalidatePhpIni()` (`forceRegenerate: true`), which the settings UI calls on explicit save. Users may hand-edit it. `httpd.conf` and vhost files, by contrast, are regenerated on every Apache start — hand edits there are lost.
- `ValidateConfiguration()` throws before writing anything if a server name/alias is invalid or a Hyperf vhost's proxy URL is not http(s).

## Configuration Model

`config/hamster-server.json` maps to `AppConfig` via explicit `[JsonPropertyName]` on every property; enums serialize as strings. On first launch after the rename, `ConfigManager` copies legacy `xserver.json` data when the new file is absent. Sections: `apache` (port, sslPort, serverName, ssl cert/key, `extraPorts[]`, allowLanAccess, maxClients, keepAliveTimeout, gzip/brotli/cache, customDirectives), `php` (currentVersion, fcgiPort, workers, maxRequests, memoryLimit, maxExecutionTime, opcache, timezone, `enabledExtensions[]`), `vhosts[]`, plus `serverMode`, `minimizeToTray`, `autoStart`, `startWithWindows`.

`ConfigManager.Normalize()` is the single validation choke point and runs on **both** load and save: it null-fills collections, clamps every numeric range, forces the three ports (http/ssl/fcgi) to be distinct via `FindAvailablePort`, drops extra ports colliding with them, rejects extension names that are not `[A-Za-z0-9_]`, and sanitizes timezone and vhost strings. **Any new config field needs its normalization added here** — nothing else validates config.

Saving is atomic: serialize → `*.tmp` → `File.Replace` with `hamster-server.json.bak` as the backup. On load, a corrupt primary falls back to `.bak`, restores it in place, and sets `LastLoadRecovered`; if that also fails, defaults are used and `LastLoadCorrupted` is set. `HamsterServerForm` surfaces both as message boxes at startup. `_preserveBackupOnNextSave` keeps a known-good `.bak` from being overwritten by the first save after a recovery.

`PhpSettings.CurrentVersion` is a bare directory name (`Path.GetFileName` is applied in `Normalize`, and `SwitchPhpVersionAsync` re-checks `Path.GetFileName(version) == version`) — this is the path-traversal guard, keep it.

## UI Layer

**The UI is deliberately native Windows Forms**: standard `MenuStrip` / `ToolStrip` / `StatusStrip` / `GroupBox` / `TabControl` / `ListView`, system-drawn controls, `SystemColors`. There are no custom-painted "cards" and no flat-style button theming — a change that reintroduces `Paint` handlers or hard-coded `Color.FromArgb` in a form is going the wrong way. There is exactly one owner-drawn thing, described below, and it exists to preserve meaning the OS throws away, not to restyle anything.

- `UiTheme.cs` is the design system, and it only holds colors the OS has no opinion about: service status, log severity, config syntax highlighting, and the invalid/warning field backgrounds. Everything else comes from `SystemColors` and visual styles. Add new colors there; never inline `Color.FromArgb` in a form.
- Colour carries meaning, never decoration: green runs, red stops or errors, amber is transitional or risky, blue is a neutral action. Icons come from `UiTheme.CreateGlyph` — a Segoe Fluent Icons / Segoe MDL2 Assets codepoint (see `UiTheme.Glyph`) drawn into a tinted bitmap, with `CreateStatusDot` as the fallback when neither font is installed. No icon is owner-drawn; every one of them is just an `Image` handed to a stock control.
- **A selected `ListView` row loses its per-cell colour.** Windows paints the selected row with the system selection colours and ignores `SubItem.ForeColor`, and the Apache row is selected from startup — so the status colour was missing on exactly the row the user acts on. `UiTheme.DrawStatusListSubItem` is the single owner-draw implementation that fixes it: wire a list with `OwnerDraw = true`, `DrawColumnHeader` → `e.DrawDefault = true`, an **empty** `DrawItem` handler, and `DrawSubItem` → the helper. It reproduces the system rendering (system colours, icon placement, grid lines) and only changes the selected row's text colour, via `UiTheme.OnHighlight`, which lightens the three status colours enough to read on `SystemColors.Highlight`. `lvServices` and `lvVHosts` use it. Owner drawing also disables the control's own grid lines and flicker handling — the helper redraws the lines, and the list needs `LVS_EX_DOUBLEBUFFER` (see `HamsterServerForm.OnShown`).
- **Never dispose a bitmap after adding it to an `ImageList`.** The list keeps a reference and only copies when its handle is created — when the `ListView` is first shown — so an early dispose throws "Parameter is not valid" from `ImageList.CreateHandle`. Give the `ImageList` to `components` and let the form's disposal take both.
- `HamsterServerForm` is the only place that constructs the managers. Layout: menu + toolbar on top, a horizontal `SplitContainer` holding the **Services** `ListView` (Module / Status / PID / Port(s) / Version, two fixed rows — `RowApache = 0`, `RowPhp = 1`) over the **Log** group, and a `StatusStrip` at the bottom. The Start/Stop/Configure buttons act on the *selected* row (`SelectedServiceRow`), while the Service menu keeps per-service commands; both paths share the same handlers.
- Logs arrive on background threads, get queued (`_logQueue`, capped at 5000 entries, oldest dropped) and are flushed by a 50 ms `Forms.Timer` in batches; the RichTextBox is trimmed to `MaxLogLines = 2000` by cutting the first half. A 10 s health timer detects crashed processes. The **log filter only affects the pane** — every entry still reaches the log file, and disk writes are chained onto `_logWriteChain` (background, order-preserving) rather than run on the UI thread.
- `OnStatusChanged` must stay guarded against a destroyed handle: killing the processes on exit fires their `Exited` handlers on the thread pool, and marshalling to a dead form throws where nothing catches it.
- `Forms/`: `SettingsForm` (Apache/PHP/General tabs, each a `TableLayoutPanel` of `GroupBox`es + PHP extension picker built from `php/<ver>/ext/php_*.dll`), `VHostForm` + `VHostDialog` (vhost CRUD with framework presets), `PortsForm` + `PortDialog` (primary and extra ports; `PortDialog` uses static factory methods per mode and hides rows via `HideRow`/`ShrinkBy` — `ShrinkBy` assumes `BodyRowHeight` matches the designer's `RowStyle`), `ApacheConfigForm` and `PhpConfigForm` (config viewers with syntax highlighting).
- Every list-management dialog wires a `Resize` handler that grows the last `ListView` column into the leftover width. `ListView` status text is written through `SubItems[n]` — the indices are positional, so reordering columns in a designer means updating those writes.
- `Program.cs` enforces a single instance with a named mutex and writes unhandled exceptions from both the UI thread and background threads to `config/logs/crash.log`.

- Sizes that the layout engine does not scale must be measured at runtime, or they break once the display does. `CheckedListBox.ColumnWidth` is raw pixels: `SettingsForm.LayoutExtensionColumns` measures the widest extension name in the current font and re-runs on `FontChanged`, which is what a DPI change raises. The Settings dialog is sized so all extensions fit without horizontal scrolling — a `MultiColumn` `CheckedListBox` cannot be scrolled sideways with the wheel, so anything past the right edge is effectively unreachable.
- The app runs `PerMonitorV2` (`ApplicationHighDpiMode` in `HamsterServer.csproj`); every form uses `AutoScaleMode.Font`, so layouts follow a scale change instead of being bitmap-stretched by the OS.

Designer files are hand-written rather than round-tripped through the Visual Studio designer. Two conventions matter: a `Dock = Fill` control must be added to `Controls` **before** its `Top`/`Bottom` siblings, and each `NumericUpDown` needs its `BeginInit`/`EndInit` pair.

## Testing

`HamsterServer.Tests` is MSTest; `TestDirectory` gives each test a disposable temp root. `RuntimeSmokeTests` are marked `[TestCategory("Integration")]`, locate the repo root by walking up to `HamsterServer.slnx`, and require the LFS runtimes: one validates a freshly generated `httpd.conf` with `httpd.exe -t`, the other asserts `php.exe -n -v` reports the exact pinned version for **every** bundled build.

**Upgrading a bundled runtime therefore touches three places**: the `php/<version>/` (or `apache/`) directory, the version list in `HamsterServer.Tests/RuntimeSmokeTests.cs`, and the archive URL + SHA-256 table in `THIRD_PARTY_RUNTIMES.md`. Replace runtimes as whole directories — never mix DLLs across patch versions.

There is no automated UI coverage; manually exercise startup, start/stop/restart, PHP version switching, settings persistence, and vhost changes when touching those paths.

## Conventions

`.editorconfig` sets 4-space indent, CRLF, final newline, trimmed trailing whitespace, file-scoped namespaces, and system-usings-first. Nullable and implicit usings are enabled in both projects. Keep process/config behavior in `Core/` and event handling in forms.

Existing comments mix English and Vietnamese, and several carry `Fix N.N:` markers referencing a prior tuning audit — leave those markers intact when editing nearby code.
