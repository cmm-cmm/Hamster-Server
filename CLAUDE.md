# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hamster Server is a Windows-only WinForms desktop app (.NET 10) that bundles Apache 2.4 + four PHP builds (8.2/8.3/8.4/8.5) into a local dev server. The GUI starts/stops the stack, switches PHP versions, downloads new PHP runtimes from php.net, manages virtual hosts and ports, and edits Apache/PHP settings so the user never touches config files by hand.

`AGENTS.md` holds the commit/PR conventions and coding-style rules; `README.md` describes the shipped feature set; this file covers the parts you can only learn by reading several files at once.

## Build, Test, Run

Everything runs from the repo root on Windows. `global.json` pins SDK `10.0.400` (rollForward: latestPatch); `HamsterServer.csproj` pins `RuntimeFrameworkVersion` `10.0.11`.

```powershell
dotnet restore HamsterServer.slnx
dotnet build   HamsterServer.slnx
dotnet test    HamsterServer.slnx
dotnet run     --project HamsterServer/HamsterServer.csproj
dotnet publish HamsterServer/HamsterServer.csproj -c Release
dotnet format  HamsterServer.slnx --verify-no-changes   # must produce no diff
```

The project is hosted on **GitHub** (`github.com/cmm-cmm/Hamster-Server`). `.github/` contains only issue/PR templates and `FUNDING.yml` — there is **no CI workflow**, so restore, format check, test and publish are enforced by nothing but you. Any pipeline added later needs Git LFS enabled on checkout, or every runtime test fails against pointer files.

Running a subset of tests:

```powershell
dotnet test HamsterServer.slnx --filter "TestCategory!=Integration&TestCategory!=Soak"   # unit tests only
dotnet test HamsterServer.slnx --filter "TestCategory=Soak" -e HAMSTER_SOAK=1             # load tests; app must be closed
dotnet test HamsterServer.slnx --filter "FullyQualifiedName~ConfigManagerTests"
dotnet test HamsterServer.slnx --filter "Name=Constructor_PopulatesExtensionsBeforeWindowHandleExists"
```

`dotnet test` with no filter runs unit and runtime smoke tests and reports the five soak tests as **Skipped**: `FastCgiSoakTests.RequireExplicitOptIn` calls `Assert.Inconclusive` unless `HAMSTER_SOAK=1`. They start the real stack, bind ports and kill every `httpd`/`php-cgi` of the Debug build, so they only run when asked for. Do not move that opt-in into a `.runsettings` `TestCaseFilter`: VSTest ANDs it with the command-line `--filter`, so `TestCategory!=Soak` there makes `--filter TestCategory=Soak` match nothing (verified).

The version string lives in three places that a release bump must change together: `<Version>` in `HamsterServer.csproj`, the `HamsterServer-PHP-Version-Manager/<major.minor>` User-Agent in `PhpVersionManager.cs`, and the "current development source identifies as" line in `README.md`.

**Git LFS is required.** Apache and PHP binaries (`*.dll`, `*.exe`, `*.lib`, `*.so`, `*.phar` under `HamsterServer/apache/` and `HamsterServer/php/`) are committed through LFS — see `.gitattributes`. Cloning without LFS leaves pointer files and every runtime test fails.

## Repository Layout

```
HamsterServer.slnx                 solution (Hamster Server + HamsterServer.Tests)
HamsterServer/                     the WinForms app
  Core/                      process + config logic (no UI)
  Models/                    config objects, enums, and framework profile catalog
  Forms/                     secondary dialogs
  UiTheme.cs                 the design system (colors, glyphs, owner-draw helper)
  HamsterServerForm.cs             main window
  apache/Apache24/           bundled Apache (LFS)
  php/8.2.33 ... 8.5.9/      bundled PHP builds (LFS), dir name == version
  config/                    runtime config; generated files are gitignored
  www/                       default document root - also the PHP benchmark dashboard app
ssl/                         cacert.pem + OpenSSL assets, linked into the app output
doc/PERFORMANCE_EVALUATION.md      measured results and the open FastCGI stability issue
THIRD_PARTY_RUNTIMES.md      pinned runtime archives + SHA-256
```

`apache/`, `php/`, `config/`, `www/` and the repo-root `ssl/` are `Content` items copied `PreserveNewest` into the build output, so **the running app operates on `HamsterServer/bin/<cfg>/net10.0-windows/`, not on the source tree**. Note `ssl/` lives at the repo root and is pulled in with `Include="..\ssl\**\*" Link="ssl\..."` — code resolves it as `<rootDir>/ssl/cacert.pem`, so it must sit next to the exe. Generated `httpd.conf`, `vhosts/*.conf`, `php.ini`, and logs appear under the output directory. Editing source `config/` only affects the next build.

`HamsterServer/www/` is not a placeholder: `index.php`, `api/benchmark.php`, `src/BenchmarkSuite.php` and `assets/` implement the load-test dashboard whose numbers `doc/PERFORMANCE_EVALUATION.md` reports.

## Root Directory Resolution

`HamsterServerForm.ResolveRootDir()` does **not** look for `HamsterServer.csproj`. It accepts `AppContext.BaseDirectory` if it contains a `php/` directory plus one of `apache/`, `Apache/`, `Apache24/`; otherwise it walks up at most 5 parents looking for the same shape, and falls back to `BaseDirectory`. Everything else (`ConfigManager`, `PhpVersionManager`, `ServerManager`, `LogWriter`) is constructed with that one `rootDir` string and derives all paths from it.

## Core (`HamsterServer/Core`)

| Class | Responsibility |
|---|---|
| `ServerManager.cs` | Owns the `httpd.exe` and `php-cgi.exe` processes: start/stop/restart, health, watchdog, `SynchronizePhpIni()`. Raises `LogReceived` / `StatusChanged`. |
| `ApacheConfigWriter.cs` | Generates `httpd.conf`, `config/vhosts/*.conf`, and each runtime's `php.ini` from `AppConfig`. |
| `ConfigManager.cs` | Loads/saves `config/hamster-server.json` atomically, normalizes it, and owns the "Start with Windows" registry value. |
| `PhpVersionManager.cs` | Scans `php/*/php-cgi.exe`, and downloads/installs/deletes runtimes from php.net. |
| `PhpExtensionManager.cs` | Discovers `ext/php_*.dll`, owns the `RecommendedExtensions` profile, resolves the configured selection per runtime. |
| `LogWriter.cs` | Background channel writer to daily `config/logs/server/hamster-server-yyyy-MM-dd.log`, prunes files older than 30 days on date rollover. |
| `LogEntryFormatter.cs` | Splits a log line into semantic spans (timestamp / level / source / link) for the UI, without altering the persisted text. |

### Startup sequence

`StartAllAsync` launches PHP and Apache **concurrently** — Apache connects to FastCGI per-request, so there is no ordering dependency. `StopAllAsync` is ordered (Apache first, 300 ms, then PHP).

`StartApacheCoreAsync` does, in order: locate Apache dir → kill orphaned `httpd.exe` **belonging to this installation only** (matched by full exe path) → pre-flight check every configured port and abort with the owning process name on conflict → regenerate `httpd.conf` + vhosts → `httpd.exe -t -f <conf>` validation → spawn. The pre-flight reads the OS listener table once (`GetActiveTcpListeners`) instead of probing each port with a TCP connect; `FindPortOwner` shells out to `netstat` only for ports that actually conflict.

`StartPhpCoreAsync` writes `php.ini` (see the regeneration rules below), kills matching orphaned `php-cgi`, warns when `Workers × MemoryLimit > 70%` of total system RAM (and suggests a safer worker count), then spawns `php-cgi.exe -b 127.0.0.1:<FcgiPort>` with `PHP_FCGI_CHILDREN`, `PHP_FCGI_MAX_REQUESTS` and `PHPRC` in the environment. `PHP_FCGI_MAX_CONNECTIONS` is deliberately never set (unstable on Windows).

Each worker is a process costing ~16 MB private / ~35 MB working set, so the default worker count is `Math.Clamp(ProcessorCount, 4, 8)` rather than a multiple of the core count: measured on a 12-core box, 24 workers idled at 891 MB of working set and served *fewer* requests per second at 16-way concurrency than 8 workers at 322 MB. Existing `hamster-server.json` files keep whatever they already store.

### Concurrency and lifecycle invariants

- `_apacheGate` / `_phpGate` (`SemaphoreSlim(1,1)`) serialize start/stop; public methods take the gate, `*CoreAsync` methods assume it is held. The gates are intentionally never disposed — a watchdog restart may still be parked in `WaitAsync`, and disposing under it would throw out of its `finally` release.
- `_apacheStopRequested` / `_phpStopRequested` tell the exit handlers a stop was intentional, so the watchdog does not restart.
- PHP watchdog: on unexpected exit, auto-restart after 2 s, capped at 5 restarts per 120 s window; `_phpDesiredRunning` gates it and `SchedulePhpRestart` cancels any superseded attempt.
- `Dispose()` is synchronous (no `await`): kills tracked process trees, then sweeps `Process.GetProcessesByName("httpd"/"php-cgi")` and kills only those whose image path matches this installation — mpm_winnt child processes and PHP workers routinely outlive their parents.
- That path comparison goes through `TryGetProcessImagePath` (`QueryFullProcessImageName`), **not** `Process.MainModule`. `MainModule` enumerates every loaded module of the target, which made stopping PHP take ~19 s with 24 workers. Every `Process` handed back by `GetProcessesByName` is disposed, matching or not.

### Connection reuse and worker recycling are mutually exclusive

`enablereuse=on` combined with `PHP_FCGI_MAX_REQUESTS=1000` returned occasional HTTP 503 (`AH01067`, `AH01075`, Windows error 10054) because Apache can reuse a socket a recycling worker just closed — 3 failures in 12 000 requests at 32-way concurrency, versus 0 with recycling off at the same throughput. `ttl` does not close that race.

The two mechanisms are therefore wired to exclude each other: **`Php.MaxRequests == 0` (the default) means workers never recycle, and only then does `ApacheConfigWriter.FcgiReuse` emit pooling.** Setting it above 0 is an explicit opt-in to recycling and drops the reuse parameters entirely, falling back to Apache's no-pooling default — slower, but never a stale socket. `ConfigManager.Normalize` maps any value `<= 0` to `0` (it must never clamp up to `1`, which would recycle after every request while pooling stayed on) and upgrades the old default of `1000` to `0` once. See `doc/PERFORMANCE_EVALUATION.md`.

### Memory watchdog — what replaces request-count recycling

Never recycling leaves nothing to reclaim what a script or extension leaks, so the job moves from counting requests to measuring memory. A `System.Threading.Timer` in `ServerManager` sweeps every 60 s and restarts the whole pool — **only while Apache is serving nothing**, so no request is cut in half.

The threshold comes from `EffectiveMemoryThresholdMb`: either `Php.MemoryRestartThreshold` (MB, `0` disables), or — when `Php.MemoryRestartAuto` is on, which is the default — `AutoMemoryThresholdMb(workers)` = `workers × WorkerBaselineMb × AutoThresholdFactor`, floored at 512 MB and capped at half of RAM. A fixed number cannot serve both ends: 1024 MB is six times the floor of a 4-worker pool but only twice that of a 12-worker one, and at twice the floor the pool is recycled every time it does real work. `WorkerBaselineMb = 40` is measured, not guessed — ~35 MB when the worker count was tuned, and 40.2 MB across nine recycles of a 12-worker pool in production.

`SuggestWorkers(memoryLimitMb)` is the matching advice for the worker count itself, used by both the hint beside the field and the "Apply mode defaults" button so the two can never disagree. CPU sets the useful ceiling (`Clamp(ProcessorCount, 4, 8)` — more is measurably worse); RAM sets the hard one, cutting the suggestion to fit `workers × memoryLimit` inside 70% of RAM, and `WorkerAdvice.RamLimited` says which limit bound so the UI can explain itself.

- `GetPhpPoolWorkingSetBytes()` sums `WorkingSet64` across every `php-cgi` whose image path matches this installation, via the same `TryGetProcessImagePath` comparison the kill sweeps use. The tracked parent is a supervisor; the memory lives in the workers.
- `IsPhpPoolIdleAsync()` reads `BusyWorkers` from `mod_status` at `http://127.0.0.1:<port>/server-status?auto` and treats `<= 1` as idle (the poll itself occupies one worker). **Counting TCP connections to the FastCGI port does not work and must not be reintroduced**: `ttl=15` only discards a pooled connection when Apache goes to reuse it — there is no background reaper — so with `enablereuse=on` those sockets sit `ESTABLISHED` forever and a pool that ever served traffic would never look idle. This was caught by a test, not by review.
- `WriteHttpdConf` therefore emits a `<Location /server-status>` block with `SetHandler server-status` and `Require ip 127.0.0.1` / `::1`. It stays loopback-only even when `AllowLanAccess` opens everything else. `ExtendedStatus` is deliberately left off — `BusyWorkers` is in the basic output and costs nothing.
- While waiting for that gap the poll is tight (250 ms, `PhpMemoryIdlePollMs`), and that is load-bearing: even a machine under steady traffic has gaps between requests, and a 5 s poll walked straight past them. Measured with one request every 200 ms running continuously, the tight poll still found a gap and recycled with **zero** failed requests; the 5 s version would have waited out the whole window. The sweep interval itself also tightens from 60 s to 15 s once the pool is within 80% of the threshold (`RescheduleSweep`).
- Guards: a 10-minute cooldown after a successful restart, plus `CheckThresholdIsReachable`, which compares the working set **immediately after** a recycle against the threshold and stops the watchdog when that floor is already at 80% of it — the only condition under which recycling can never help. **Do not replace this with a "max N restarts per hour" budget**; that is what it used to be, and production logs killed it: a leaky application needed seven recycles across a working day, two of them 47 minutes apart, which the old rule read as a fault and answered by switching the watchdog off for an hour. The cooldown already caps the rate.
- After `MaxMissedWindowsBeforeWarning` consecutive checks that never caught a gap, the log escalates from INFO to a WARN naming the current usage and telling the user to restart PHP themselves or raise the threshold. Refusing to cut a live request is the policy, but silently never reclaiming memory is not an outcome to leave unsaid.
- The threshold is read live. `ServerManager.ApplyPhpMemorySettings()` (called by `HamsterServerForm` after the settings dialog saves) re-arms the timer and resets the rate-limit window, so changing it never bounces a healthy pool. It is deliberately **not** part of `SettingsForm.NeedsPhpRestart`.

## Generated Configuration — hard constraints

These rules are load-bearing; violating them produces crashes or `ERR_INVALID_RESPONSE` rather than clean errors.

- **One port per `<VirtualHost>`.** `<VirtualHost *:80 *:8080>` crashes Apache instantly on mpm_winnt. `WriteAllVHosts` therefore emits one file per port: `00-default.conf` (main port), `20-port-<port>.conf` / `20-port-ssl-<port>.conf` (extra ports, skipped when a custom vhost already claims that port), `10-<slug>-<sha256[0..10]>.conf` (custom vhosts). Files not in the keep-set are deleted, so stale vhosts self-clean; a locked file is skipped rather than aborting the start.
- `Models/FrameworkProfile.cs` is the single source of truth for framework labels, public web-root discovery, project markers, PHP extension requirements, front-controller behavior, and sensitive-file patterns. Both `VHostDialog` and `ApacheConfigWriter` consume it; never add a preset independently to either class. Append new `FrameworkPreset` numeric values instead of reordering existing ones, because the JSON enum converter also accepts legacy numeric values. `FrameworkProfiles.BuiltInModules` lists modules compiled into the bundled Windows builds, so `GetMissingManagedExtensions` never nags about them.
- **`display_errors` must always be `Off`.** Any PHP output before headers corrupts the FastCGI response. Errors go to `config/logs/php_error.log`; `output_buffering = 4096` is set for the same reason.
- **FastCGI is wired per-VirtualHost, not globally**, so `DOCUMENT_ROOT` resolves correctly. Each block sets `ProxyFCGIBackendType GENERIC`, a `ProxyPassMatch` to `fcgi://127.0.0.1:<FcgiPort>/$1`, plus a `RewriteRule` that stashes the real filesystem path in `PHP_FCGI_SCRIPT` and a `ProxyFCGISetEnvIf` that feeds it back as `SCRIPT_FILENAME` — this is what makes document roots containing spaces work.
- **`ProxyPassMatch` carries `enablereuse=on max=<Workers> ttl=15` (`ApacheConfigWriter.FcgiReuse`) whenever `Php.MaxRequests == 0`, and all three parts are load-bearing.** `mod_proxy_fcgi` defaults to no reuse, opening and discarding a TCP connection to `php-cgi` for *every* PHP request: latency 0.593 vs 0.287 ms, and a 20 000-request / 64-way soak ran at 1 023 vs 5 333 req/s. The mechanism shows in the socket table — 16 099 sockets left in `TIME_WAIT` toward the FastCGI port after 20 s of load, against a default Windows ephemeral range of 16 384. But `enablereuse=on` **alone is worse than no reuse at all**: `max` defaults to `ThreadsPerChild` (256), so the pool fills with connections no php-cgi child is serving and requests die on `AH01075 ... (polling)` timeouts — at 32-way concurrency throughput fell from 13 066 to **13 req/s with 10 failed requests**. Pinning `max` to the worker count restores it (~18 000 req/s, zero failures at 8–128 way), which is why `max` must track `Php.Workers` and not be hard-coded. The property returns its own leading space so the directive has no trailing whitespace when reuse is off. When testing this, restart `php-cgi` between variants: a run that poisoned the backend makes the next variant look broken.
- `opcache.jit = function` (tracing mode can crash on mpm_winnt).
- Browser caching (`mod_expires`) is deliberately restricted to **static asset MIME types** (images, fonts, css, js) via `ExpiresByType`. Do not add `ExpiresDefault` — caching a PHP response makes local development confusing and stale.
- `WriteIfChanged` compares content with normalized line endings before writing, so a restart does not touch unchanged files.
- `ValidateConfiguration()` throws before writing anything if a server name/alias is invalid, two enabled vhosts share a `ServerName`, or a Hyperf vhost's proxy URL is not http(s).
- `httpd.conf` and vhost files are regenerated on **every** Apache start — hand edits there are always lost.

### php.ini regeneration rules

`ApacheConfigWriter.WritePhpIni(phpDir, configDir, forceRegenerate)` writes a *per-runtime* `php.ini` into `php/<version>/`. It is deliberately conservative — users may hand-edit the file — and decides as follows:

1. Missing file → write it.
2. Exists, and the first 8 lines contain neither `PhpIniGeneratedHeader` nor `LegacyPhpIniGeneratedHeader` (the old XServer banner) → **return without touching it.** A hand-authored `php.ini` is never overwritten.
3. Generated, but missing the current `PhpIniSchemaMarker` (`"; Hamster Server php.ini schema: 2"`) → regenerate. **Bumping that constant is the migration lever**: it is how a template change reaches users who already have a generated file.
4. Generated and current schema, but `extension_dir` does not point at this runtime's `ext/` → regenerate. Generated files hold absolute Windows paths, so this is what repairs the portable folder after it is copied or moved.
5. `forceRegenerate: true` → always write.

`ServerManager.SynchronizePhpIni()` calls it with `forceRegenerate: true` for **every** installed runtime (any `php/*/` containing `php-cgi.exe`), not just the selected one — that is what keeps the profile identical across versions. `HamsterServerForm` calls it after a settings save that needs a PHP restart, after a vhost change that enabled extensions, after PHP Configuration saves, and after the version manager installs a runtime. It **never throws** and returns whether every runtime was written: each caller is an `async void` handler where an exception (a php.ini held open by an editor or antivirus) escaped as the crash box and abandoned the restart half-way. A failing runtime is logged and skipped.

### PHP extensions

`PhpExtensionManager` is the only place that decides what lands in `php.ini`:

- `RecommendedExtensions` is the XAMPP-compatible profile and doubles as the `PhpSettings.EnabledExtensions` default. It is verified by an integration test that loads every name in **every bundled PHP build** with zero stderr output — adding a name here without checking all four runtimes breaks `dotnet test`.
- `ResolveForRuntime` emits only extensions whose DLL exists in the selected runtime, which keeps one global selection portable across versions; if `ext/` cannot be read it returns the configured names untouched rather than silently emptying the profile.
- `opcache` is excluded everywhere and handled separately as `zend_extension = opcache`, gated on `HasOpcacheDll`.

## PHP Version Manager

`PhpVersionManager` is both the local scanner and the php.net installer (`Forms/PhpVersionManagerForm` is its UI, reached from the main window). Everything here is security-sensitive — it downloads and unpacks executables:

- Catalog: `https://windows.php.net/downloads/releases/releases.json`, which since 2026 answers with a 302 to `https://downloads.php.net/~windows/releases/releases.json` (HttpClient follows it). Package URLs are resolved against the catalog's **final** URL, so downloads go straight to downloads.php.net. Filtered to the `nts-*-x64` package of each branch; a release is kept only if its version is a valid `PhpVersionKey`, the package path is a bare `*.zip` filename, and the `sha256` field is 64 hex chars.
- **Versions are `PhpVersionKey`, never `System.Version`.** windows.php.net publishes Windows rebuilds as `8.5.12-1` (same PHP version, rebuilt binaries); from October 2026 every supported branch carries such a suffix. `Version.TryParse` rejects it, and the old `^\d+\.\d+\.\d+(\.\d+)?$` filter silently dropped 8.2–8.5 from the catalog, so an install on 8.5.9 never saw 8.5.12. `PhpVersionKey` orders `8.5.9 < 8.5.12 < 8.5.12-1 < 8.5.12-2`, so a rebuild is an update too; it is also the folder-name guard (`IsSafeVersion`). Installed folders keep the catalog's name verbatim (`php/8.5.12-1/`); the bundled runtimes have no suffix.
- Every response is re-checked with `EnsureTrustedPhpUri` against `response.RequestMessage.RequestUri`, so a redirect off `windows.php.net` / `downloads.php.net` (or off HTTPS) aborts. `ValidateRelease` repeats the check before download.
- Install is staged: download → `SHA256` compared with `CryptographicOperations.FixedTimeEquals` → extract → `Directory.Move` into `php/<version>/`. The staging folder is `php/.hamster-server-install-<guid>`, and `Scan()` skips any `.hamster-server-*` directory, so a partial install can never appear as a usable runtime. The `finally` block always deletes it.
- `ExtractZipSafelyAsync` resolves every entry's full path and rejects anything outside the destination (zip-slip), and opens targets with `FileMode.CreateNew`. `FindRuntimeDirectory` requires exactly one `php-cgi.exe` in the archive, and `php.exe` must exist.
- `GetVersionDirectory` is the path-traversal guard for both install and delete: safe filename, no `.hamster-server-` prefix, and the resolved path must stay under `php/`.
- `DeleteVersion` refuses to remove the currently configured version, refuses to leave fewer than one runtime installed, and refuses a folder without `php-cgi.exe`.
- `GetLatestForBranch` / `GetUpdate` match on major+minor, which is how updates install side-by-side within a branch instead of jumping across minors. `HamsterServerForm` uses `GetLatestForBranch` at startup to recover when the configured version is gone.
- `HttpClient` is injectable and `ParseReleasesAsync` is a static `Stream` overload — that is how `PhpVersionManagerTests` covers catalog parsing and the guards without network access.

### Patch updates and extension carry-over

- **Extension DLLs follow the user across patches, never across branches.** `PhpVersionManager.FindMissingFiles` / `CopyMissingFiles` compare an installed runtime against the other installed runtimes of the **same major.minor** and copy `ext\*.dll` plus root-level dependency DLLs (imagick's `CORE_RL_*`) the target lacks — in practice what the user dropped in by hand (sqlsrv, pdo_sqlsrv, xdebug). Root files named `php*` are the core and never copied; a file the target already has is never overwritten, so a DLL shipped with the newer patch wins. Cross-branch copying is refused because extension DLLs are built per minor (ZEND_MODULE_API_NO). php.ini needs nothing copied: it is regenerated from AppConfig.
- `Core/PhpUpdateService` keeps the active runtime on the latest patch of its branch. `Php.UpdateMode`: `Auto` (default) installs side-by-side, carries extensions over, runs `SynchronizePhpIni`, then switches — waiting for `IsPhpPoolIdleAsync` like the memory watchdog, so no request is cut; `Notify` only reports (status-bar link `lblStatusPhpUpdate`, one tray balloon per version); `Off` never contacts php.net. It never moves to another major.minor. The old patch is kept for rollback; "Remove old patches" (`GetSupersededVersions`) deletes them on request. The main form checks 60 s after start-up and every 6 h (`InitPhpUpdateTimer`) and re-checks right after the mode changes.
- The service is UI-thread affine on purpose (called from a `Forms.Timer`, every await resumes on the UI thread), so its state is never touched by two threads; `InstallAsync` is serialised by `_installGate` because the background updater and the manager window can both start an install. A user who switches version while the updater waits for idle wins: it re-checks `CurrentVersion` before switching.
- Manual updates in `PhpVersionManagerForm` go through the same `ApplyUpdateAsync(…, switchImmediately: true)`, so a click carries extensions over exactly like the background path. Installing a newer patch of an installed branch counts as an update of that branch; another branch installs alone. The form reads `ListViewItem.Selected` rather than `ListView.SelectedItems`, which stays empty until the native control exists.

## Configuration Model

`config/hamster-server.json` maps to `AppConfig` via explicit `[JsonPropertyName]` on every property; enums serialize as strings. On first launch after the rename, `ConfigManager` copies legacy `xserver.json` data (and its `.bak`) when the new file is absent, then deletes the `xserver.json*` leftovers. Sections: `apache` (port, sslPort, serverName, enableSsl, ssl cert/key, `extraPorts[]`, allowLanAccess, maxClients, keepAliveTimeout, gzip/brotli/cache, customDirectives), `php` (currentVersion, fcgiPort, workers, maxRequests, memoryRestartAuto, memoryRestartThreshold, memoryLimit, maxExecutionTime, uploadMaxFilesize, postMaxSize, shortOpenTag, errorReporting, opcache, timezone, useBundledCaCertificate, updateMode, `enabledExtensions[]`), `vhosts[]`, plus `serverMode`, `minimizeToTray`, `autoStart`, `startWithWindows`.

`ConfigManager.Normalize()` is the single validation choke point and runs on **both** load and save. **Any new config field needs its normalization added here** — nothing else validates config. It currently:

- null-fills collections and clamps every numeric range;
- forces the three ports (http/ssl/fcgi) to be distinct via `FindAvailablePort`, and drops extra ports colliding with them;
- rejects extension names that are not `[A-Za-z0-9_]`, strips `opcache`, dedupes and sorts them, and **upgrades the exact legacy default set to `PhpExtensionManager.RecommendedExtensions`** (`LegacyDefaultExtensions.SetEquals`) so old configs pick up the wider profile once;
- maps `Php.MaxRequests <= 0` to `0` ("never recycle") instead of clamping up into `1..1_000_000`, and upgrades the legacy default of `1000` to `0` once (`LegacyDefaultMaxRequests`) — the same one-shot migration pattern as the extension set;
- maps non-positive `Php.UploadMaxFilesize` / `PostMaxSize` to `128` (the value the template hard-coded before they became settings) and clamps them into `1..4096`; resets an undefined `Php.ErrorReporting` to `Auto` and an undefined `Php.UpdateMode` to `Auto`;
- clamps `Php.MemoryRestartThreshold` into `128..65_536`; `<= 0` means "watchdog off" when `MemoryRestartAuto` is off, but falls back to `1024` when it is on, so turning Auto off hands back a usable number instead of silently disabling the watchdog;
- sanitizes timezone and vhost strings;
- resets `vhost.Port` to `0` (meaning "main port") when it is not one of the ports Apache will actually listen on, and resets an undefined `Framework` to `None`.

Saving is atomic and gated by `_saveGate` (`Save` and `SaveAsync` share it): serialize → `*.tmp` → `File.Replace` with `hamster-server.json.bak` as the backup. On load, a corrupt primary falls back to `.bak`, restores it in place, and sets `LastLoadRecovered`; if that also fails, defaults are used and `LastLoadCorrupted` is set. `HamsterServerForm` surfaces both as message boxes at startup. `_preserveBackupOnNextSave` keeps a known-good `.bak` from being overwritten by the first save after a recovery.

`PhpSettings.CurrentVersion` is a bare directory name (`Path.GetFileName` is applied in `Normalize`, and `SwitchPhpVersionAsync` re-checks `Path.GetFileName(version) == version`) — this is the path-traversal guard, keep it.

Two independent TLS settings exist and are easy to confuse: `apache.sslCertFile`/`sslKeyFile` are Apache's **server** certificate, while `php.useBundledCaCertificate` writes absolute `curl.cainfo` / `openssl.cafile` paths pointing at `<rootDir>/ssl/cacert.pem` so **outbound** PHP HTTPS calls validate.

## UI Layer

**The UI is deliberately native Windows Forms**: standard `MenuStrip` / `ToolStrip` / `StatusStrip` / `GroupBox` / `TabControl` / `ListView`, system-drawn controls, `SystemColors`. There are no custom-painted "cards" and no flat-style button theming — a change that reintroduces `Paint` handlers or hard-coded `Color.FromArgb` in a form is going the wrong way. There is exactly one owner-drawn thing, described below, and it exists to preserve meaning the OS throws away, not to restyle anything.

- `UiTheme.cs` is the design system, and it only holds colors the OS has no opinion about: service status, log severity, config syntax highlighting, and the invalid/warning field backgrounds. Everything else comes from `SystemColors` and visual styles. Add new colors there; never inline `Color.FromArgb` in a form.
- Colour carries meaning, never decoration: green runs, red stops or errors, amber is transitional or risky, blue is a neutral action. Icons come from `UiTheme.CreateGlyph`, which strokes a `UiTheme.Glyph` — **SVG path data on a 24 px grid** (2 px margin, 1.5 px stroke, round caps, never filled) — into a tinted bitmap via `SvgPath.Parse` → `GraphicsPath`. There is no icon font any more: the old Segoe Fluent / MDL2 codepoints depended on what the machine had installed and fell back to identical dots. `SvgPath` implements the full path grammar (arcs included); a dot is a near-zero segment (`h.01`) the round cap makes circular. Below 20 px the pen has a 1.25 px floor so 100 % icons do not look faded. `IconTests` checks every glyph parses, stays inside the safe margin and renders unclipped at 16–48 px; set `HAMSTER_GLYPH_SHEET=<path>.png` when running it to get a contact sheet for reviewing a glyph change by eye. `CreateStatusDot` draws the state dots. No icon is owner-drawn; every one of them is just an `Image` handed to a stock control. `LogApache` is `#9C2777`, deliberately not the `#005AB4` of `AccentAction`/`AccentHttp`; the comment above it records how it was chosen (contrast and colour-blind ΔE against every other token) — re-run that check before adding a token.
- `HamsterServerForm.InitToolbarIcons` is the single place every main-window glyph is assigned, and **every actionable item belongs in it** — toolbar buttons, menu items, submenu parents, context-menu entries, the tray menu and the plain `Button`s beside the Services list. It runs **after `InitTray`** because the tray items are created there. Dialogs assign their own glyphs in an `InitIcons` (or constructor), including their right-click menus.
- **Glyphs are rendered at `UiTheme.IconSize`**, 16 px scaled by the primary monitor's DPI and snapped to an even number, not a fixed 16 px. Every `ImageList` uses `UiTheme.IconBox`, and every `ToolStrip`/`MenuStrip`/`ContextMenuStrip`/`StatusStrip` that carries glyphs must go through `UiTheme.ApplyIconScaling`; otherwise PerMonitorV2 scales `ImageScalingSize` and resamples the bitmap, which is what made icons blurry at 150–200 %. The size is measured once at startup; moving to a monitor with another scale lets Windows resample. The four top-level menus (`File`, `Service`, `Tools`, `Help`) are the deliberate exception: Windows menu bars do not carry icons. The per-service submenus and the two config viewers reuse `UiTheme.LogApache` / `LogPhp`, which is already how the log pane distinguishes those two services.
- `UiTheme.ApplyGlyph(Button, glyph, color, autoSize)` is the shared helper for plain `Button`s (`HamsterServerForm.SetIcon` just forwards to it). `AutoSize` is required rather than cosmetic: the hand-written designer widths leave no room for a 16 px glyph, and these buttons sit in `FlowLayoutPanel`s with `WrapContents = false`, so a fixed width clips the caption instead of widening the row. Pass `autoSize: false` only for a button anchored `Left|Right` in a `TableLayoutPanel` cell — AutoSize overrides that stretch and collapses it to its text width, so widen the cell by hand instead (that is why `VHostDialog`'s profile column went 142 → 168 px). Adding a glyph means checking its container: `flpSvcRight` 283 → 360, `flpExtActions` 300 → 360 and `PhpConfigForm.flpFooter` 330 → 370 all had to grow.
- **Dialog buttons stay bare.** `OK`, `Cancel`, `Close`, `Save`, `Browse…` carry no icon — that is the Windows convention, not an oversight. Only *action* buttons get a glyph: `Recommended`, `Check all`, `Uncheck all`, `Apply mode defaults`, `Save && restart PHP`, `Enable extensions`, and the Start/Stop/Configure/Manage row beside the Services list.
- **A selected `ListView` row loses its per-cell colour.** Windows paints the selected row with the system selection colours and ignores `SubItem.ForeColor`, and the Apache row is selected from startup — so the status colour was missing on exactly the row the user acts on. `UiTheme.DrawStatusListSubItem` is the single owner-draw implementation that fixes it: wire a list with `OwnerDraw = true`, `DrawColumnHeader` → `e.DrawDefault = true`, an **empty** `DrawItem` handler, and `DrawSubItem` → the helper. It reproduces the system rendering (system colours, icon placement, grid lines) and only changes the selected row's text colour, via `UiTheme.OnHighlight`, which lightens the three status colours enough to read on `SystemColors.Highlight`. `lvServices` and `lvVHosts` use it. Owner drawing also disables the control's own grid lines and flicker handling — the helper redraws the lines, and the list needs `LVS_EX_DOUBLEBUFFER` (see `HamsterServerForm.OnShown`).
- **Never dispose a bitmap after adding it to an `ImageList`.** The list keeps a reference and only copies when its handle is created — when the `ListView` is first shown — so an early dispose throws "Parameter is not valid" from `ImageList.CreateHandle`. Give the `ImageList` to `components` and let the form's disposal take both.
- `HamsterServerForm` is the only place that constructs the managers. Layout: menu + toolbar on top, a horizontal `SplitContainer` holding the **Services** `ListView` (Module / Status / PID / Port(s) / Version, two fixed rows — `RowApache = 0`, `RowPhp = 1`) over the **Log** group, and a `StatusStrip` at the bottom. The Start/Stop/Configure buttons act on the *selected* row (`SelectedServiceRow`), while the Service menu keeps per-service commands; both paths share the same handlers.
- Log pipeline: entries arrive on background threads and fan out to two places. The pane gets `_logQueue` (capped at 5000 entries, oldest dropped) flushed by a 50 ms `Forms.Timer` in batches, colourized from `LogEntryFormatter.ParseSpans` offsets, and the RichTextBox is trimmed to `MaxLogLines = 2000` by cutting the first half. The file gets `LogWriter.Enqueue`, an unbounded `Channel` drained by one background consumer that batches bursts into a single file open (order-preserving, never on the UI thread); shutdown calls `CompleteAndFlush(2 s)`. The **log filter only affects the pane** — every entry still reaches the log file. A 10 s health timer detects crashed processes.
- `LogEntryFormatter.NormalizeForDisplay` exists because `RichTextBox` stores CRLF as LF; span offsets must be computed against the normalized text or the colouring drifts.
- `OnStatusChanged` must stay guarded against a destroyed handle: killing the processes on exit fires their `Exited` handlers on the thread pool, and marshalling to a dead form throws where nothing catches it.
- **Each setting has exactly one editor.** `SettingsForm`'s PHP tab holds the *process* settings (FastCGI port, workers, max requests, memory watchdog, OPcache, outbound CA); every *php.ini directive* (memory_limit, upload/POST sizes, execution time, error reporting, short tags, timezone) and the extension list live only in `PhpConfigForm`. Settings shows a pointer group with "Open PHP Configuration…", which calls back into `HamsterServerForm.ShowPhpConfigDialog(owner)`, and reads `Php.MemoryLimit` live for its worker hint and RAM warning. Do not re-add a php.ini field to Settings. The timezone list comes from `Core/TimezoneCatalog`.
- **Ports window = Apache ports + System ports.** `Core/SystemPortScanner` lists every listening TCP socket with its owning PID through `GetExtendedTcpTable` (IPv4 + IPv6, merged per port+PID) — not by parsing netstat's localised output — and `Classify` marks this installation's httpd / php-cgi (image path under the install root, via `ServerManager.TryGetProcessImagePath`, which is `internal` for this), a configured port held by anything else (`Conflict`), and well-known services. The Apache tab's "On this computer" column and the status-bar FastCGI note read the same snapshot; the scan runs off the UI thread on open, Refresh / Check availability and F5. `PortsForm` needs the root dir (`new PortsForm(config, _rootDir)`) — without it Hamster's own processes look like conflicts.
- **Virtual Hosts** adds Enable/Disable, Open in browser and Open folder (toolbar + context menu) and checks names against the Windows hosts file (`Core/HostsFile`, read-only; `*.localhost` always counts as resolvable): an enabled vhost whose name does not point at loopback shows "Active · not in hosts file" in amber, and the footer spells out the line to add.
- Settings tabs carry the subject glyph (Apache server / PHP code / cog) through `tabControl.ImageList`, plus one grey intro sentence added at runtime by `SettingsForm.AddIntro` (docked Top after the tab's Fill layout).
- `Forms/`: `SettingsForm` (Apache/PHP/General tabs, each a `TableLayoutPanel` of `GroupBox`es), `AboutForm` (logo + version + runtime versions + links + "copy system info"; the logo comes from `new Icon(appIcon, px, px).ToBitmap()` so it picks a real frame out of the multi-resolution icon rather than stretching one, and the caller passes `_baseIcon`, not `Icon`, to avoid the status dot), `PhpVersionManagerForm` (download/update/delete runtimes, progress + cancel), `VHostForm` + `VHostDialog` (vhost CRUD with framework presets), `PortsForm` + `PortDialog` (primary and extra ports; `PortDialog` uses static factory methods per mode and hides rows via `HideRow`/`ShrinkBy` — `ShrinkBy` assumes `BodyRowHeight` matches the designer's `RowStyle`), `ApacheConfigForm` and `PhpConfigForm` (config viewers with syntax highlighting, reached from the Services right-click menu, double-click and the Tools menu).
- **`PhpConfigForm` edits `AppConfig`, never php.ini text.** It used to patch php.ini directly, and every `SynchronizePhpIni()` (settings save, version switch, vhost change, runtime install) force-regenerated the file from `AppConfig` and silently wiped the changes — that was the "extensions won't stick" bug. It now writes `Php.*` fields and calls back into `HamsterServerForm.ApplyPhpConfigAsync` (save → `SynchronizePhpIni` → optional PHP restart). Its extension set is the global one: names whose DLL this runtime lacks are kept, and OPcache is never listed (it is a `zend_extension`). `display_errors` is shown disabled — it is not a setting.
- **`ApacheConfigForm` validates custom directives before saving** via `ServerManager.TestApacheConfigAsync`, which writes a throw-away `config/.httpd-test-<guid>.conf` from a JSON clone of the live config and runs `httpd -t` on it. Without that, a typo saved while Apache ran stopped Apache and then failed the pre-start test, leaving it down. Saving unchanged text is a no-op instead of an Apache restart. `RunCaptureAsync` has a 30 s timeout so a hung `httpd -t` cannot hold `_apacheGate` forever.
- `SettingsForm` reports `ConfigChanged`, `NeedsApacheRestart` and `NeedsPhpRestart`; the main form uses those to decide between "saved — start servers to apply" and restarting only the affected service.
- Every list-management dialog wires a `Resize` handler that grows the last `ListView` column into the leftover width. `ListView` status text is written through `SubItems[n]` — the indices are positional, so reordering columns in a designer means updating those writes.
- `Program.cs` enforces a single instance with a named mutex and writes unhandled exceptions to `config/logs/crash.log` from three sources: `Application.ThreadException` (UI), `AppDomain.UnhandledException` (background), and `TaskScheduler.UnobservedTaskException` — faults inside fire-and-forget watchdog restarts and log flushes surface nowhere else.
- Sizes that the layout engine does not scale must be measured at runtime, or they break once the display does. `CheckedListBox.ColumnWidth` is raw pixels: `PhpConfigForm.LayoutExtensionColumns` measures the widest extension name in the current font and re-runs on `FontChanged`, which is what a DPI change raises. A `MultiColumn` `CheckedListBox` cannot be scrolled sideways with the wheel, so anything past the right edge is effectively unreachable.
- The app runs `PerMonitorV2` (`ApplicationHighDpiMode` in `HamsterServer.csproj`); every form uses `AutoScaleMode.Font`, so layouts follow a scale change instead of being bitmap-stretched by the OS.

Designer files are hand-written rather than round-tripped through the Visual Studio designer. Two conventions matter: a `Dock = Fill` control must be added to `Controls` **before** its `Top`/`Bottom` siblings, and each `NumericUpDown` needs its `BeginInit`/`EndInit` pair.

## Testing

`HamsterServer.Tests` is MSTest; `TestDirectory` gives each test a disposable temp root. The test project sets `_GetChildProjectCopyToOutputDirectoryItems=false` so the project reference does not duplicate ~400 MiB of Apache/PHP into the test output — which is why the integration tests locate the repo root by walking up to `HamsterServer.slnx` and run the bundled binaries in place. All six require the LFS runtimes:

| `RuntimeSmokeTests` (`[TestCategory("Integration")]`) | Asserts |
|---|---|
| `BundledApache_AcceptsGeneratedConfiguration` | a freshly generated `httpd.conf` passes `httpd.exe -t` |
| `BundledApache_AcceptsEveryFrameworkProfile` | a vhost for *every* `FrameworkProfiles.All` entry still passes `httpd.exe -t` |
| `BundledApache_AcceptsConfigurationWithWorkerRecycling` | the no-pooling variant (`MaxRequests > 0`) is also valid and emits no `enablereuse` |
| `BundledPhpExecutables_ReportExpectedVersions` | `php.exe -n -v` reports the exact pinned version for every bundled build |
| `BundledPhpRecommendedExtensions_LoadWithoutWarnings` | every `RecommendedExtensions` name plus OPcache loads in every build with empty stderr |
| `TestApacheConfig_AcceptsValidAndRejectsInvalidCustomDirectives` | `ServerManager.TestApacheConfigAsync` passes a real directive, rejects an unknown one, and deletes its throw-away conf (uses the source tree `HamsterServer/` as root) |

**Upgrading a bundled runtime therefore touches three places**: the `php/<version>/` (or `apache/`) directory, the hard-coded version array in `HamsterServer.Tests/RuntimeSmokeTests.cs` (it appears **twice** — once per PHP test), and the archive URL + SHA-256 table in `THIRD_PARTY_RUNTIMES.md`. Replace runtimes as whole directories — never mix DLLs across patch versions.

`FastCgiSoakTests` (`[TestCategory("Soak")]`) holds the tests that run the real stack, on ports 18080-18083. Every test skips unless `HAMSTER_SOAK=1` (see Build, Test, Run): it binds ports, and `ServerManager.Dispose` kills every `httpd`/`php-cgi` belonging to this installation, including a running instance of the app.

| Test | Asserts |
|---|---|
| `PooledFastCgi_ServesSustainedLoadWithoutServerErrors` | 12 000 requests at 32-way concurrency with pooling: zero 5xx, zero transport errors (measured 16 626 req/s) |
| `RecyclingWorkers_SurviveManyRecycleCyclesWithoutServerErrors` | same load with `MaxRequests = 200`, forcing ~15 recycles per worker: still zero 5xx (5 502 req/s) |
| `MemoryWatchdogReadings_ReflectTheRunningPool` | the working-set reading is non-zero and plausible, the pool reads busy while a request is in flight and idle once traffic stops |
| `MemoryWatchdog_RecyclesThePoolOnceItPassesTheThreshold` | with the threshold set below what the pool already uses, the sweep actually recycles it — proven by a changed PID, not by log text. Takes ~60 s (one sweep interval) |
| `MemoryWatchdog_RecyclesUnderContinuousTrafficWithoutFailingRequests` | the same recycle happens while one request every 200 ms runs throughout, and costs zero 5xx and zero dropped connections. Takes ~85 s |

`PhpUpdateTests` covers extension carry-over and the updater end to end against a fake php.net (`HttpMessageHandler` serving `releases.json` and the zip, optionally answering the catalog from downloads.php.net as the real redirect does), plus a fixture trimmed from the real catalog. `LiveCatalog_ListsEverySupportedBranch` (`Integration`) queries the real php.net only with `HAMSTER_LIVE_PHPNET=1`, and with `HAMSTER_LIVE_INSTALL=1` also downloads, installs and runs the newest release in a temp folder — the fixtures only know what we expected, this is the check that catches php.net changing format. `FormSnapshotTests` renders Settings (every tab), Virtual Hosts, the vhost dialog, Ports (both tabs) and the port dialog to PNGs when `HAMSTER_FORM_SNAPSHOT_DIR=<folder>` is set (`HAMSTER_SNAPSHOT_ROOT=<install root>` makes a running installation's processes show as Hamster Server in the Ports window). Set `HAMSTER_FORM_SNAPSHOT=<path>.png` when running `PhpVersionManagerFormTests` to render the manager window to an image for reviewing its layout (it then also queries the real php.net).

UI coverage is minimal by design: `SettingsFormTests` and `PhpVersionManagerFormTests` only assert construction-order invariants (populating controls before the window handle exists), plus that Settings no longer carries any php.ini control and that PHP Configuration hides OPcache from the extension list. There is no functional UI automation, so manually exercise startup, start/stop/restart, PHP version switching, PHP download/update/delete, settings persistence, and vhost changes when touching those paths.

## Conventions

`.editorconfig` sets 4-space indent, CRLF, final newline, trimmed trailing whitespace, file-scoped namespaces, and system-usings-first. Nullable and implicit usings are enabled in both projects. Keep process/config behavior in `Core/` and event handling in forms.

Existing comments mix English and Vietnamese, and several carry `Fix N.N:` markers referencing a prior tuning audit — leave those markers intact when editing nearby code. `doc/PERFORMANCE_EVALUATION.md` is written in Vietnamese.
