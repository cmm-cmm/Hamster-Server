# Hamster Server

Hamster Server is a Windows-only local development server that bundles Apache HTTP Server,
several PHP runtimes and a native WinForms control panel. It runs PHP applications from a
portable folder: no installer, no hand-edited `httpd.conf` or `php.ini`, and PHP kept up to
date on its own.

The current development source identifies as **Hamster Server 1.4.0** and targets
**.NET 10 for Windows**.

Hamster Server is a product of [Konex Forge](https://konexforge.com/).

## Highlights

- **PHP stays current by itself.** New patches of the PHP branch you use are installed
  side-by-side and activated while no request is running. Extensions you added (sqlsrv,
  xdebug, imagick…) come along.
- **See who is using a port.** The Ports window lists every listening port on the computer
  with its program and PID, and marks the ones that stop Apache or PHP from starting.
- **One place per setting.** Apache and process settings live in **Settings**; everything that
  ends up in `php.ini`, including extensions, lives in **PHP Configuration**.
- **Virtual hosts that tell you why they don't open.** Each site is checked against the
  Windows hosts file, and you get the exact line to add.

## Features

### Services

- Start, stop and restart Apache and PHP-CGI together or one at a time, from the toolbar,
  the Services list (right-click or double-click a row), the Service menu or the tray icon.
- Health checks detect a crashed process; PHP-CGI is restarted automatically after an
  unexpected exit (at most 5 times in 2 minutes).
- A memory watchdog restarts the PHP worker pool when it grows past a threshold derived from
  the worker count, and only while Apache is serving nothing, so no request is cut off.
- FastCGI connection pooling between Apache and PHP, sized to the worker count.
- Logs from Apache, PHP and Hamster Server in one coloured pane with filters, written to a
  daily log file as well.
- Minimise to the system tray, start the services when Hamster Server opens, or launch
  Hamster Server with Windows. The tray and taskbar icons show the overall state.

### PHP runtimes and updates

The **PHP Version Manager** (Tools menu, or **Manage…** beside the Services list) shows the
active runtime and whether it is current, every installed runtime, and every official release
you can install.

- **Updates:** choose **Install automatically** (default), **Notify me only** or **Off**.
  Hamster Server checks php.net a minute after it starts and every six hours.
  - Automatic mode installs the newer patch next to the current one, copies your extra
    extensions over, regenerates `php.ini` and switches to it once PHP is idle.
  - Updates stay within the branch you use (8.5.x → 8.5.y), including Windows rebuilds such
    as `8.5.12-1`. Moving to another branch is always your choice.
  - The previous patch is kept, so you can switch back. **Remove old patches** deletes them
    when you no longer need them.
- **Extensions follow updates.** Extension DLLs you placed in a runtime's `ext\` folder, and
  the DLLs they depend on, are copied into the next patch of the same branch. Files the new
  patch already ships are never overwritten. Extensions are not copied between branches,
  because an extension built for 8.4 does not load in 8.5.
- For each runtime, the manager shows extensions that are enabled but missing from it, and
  whether another runtime of the same branch has them (**Copy extensions**).
- **Use this version** (or a double-click) activates a runtime. You are warned first if it
  lacks an enabled extension.
- Downloads come only over HTTPS from php.net, are verified against php.net's SHA-256 checksum
  and are unpacked through a staging folder, so a failed or cancelled install never shows up
  as a runtime.

### PHP configuration

**PHP Configuration** (Tools menu, or right-click the PHP-CGI row) is the only place where
`php.ini` settings are edited:

- memory limit, upload and POST size (with a warning when POST is smaller than the upload
  limit), execution time, error reporting, short open tags and time zone;
- extensions, with a **Recommended** profile verified to load in every bundled PHP build,
  plus **Check all** / **Uncheck all** and a filter. One selection applies to every installed
  runtime.

Saving regenerates `php.ini` for every runtime and, if you choose, restarts PHP. OPcache,
worker count, request recycling, the memory watchdog and the bundled CA store for outbound
HTTPS are under **Settings › PHP**. `display_errors` stays off, because PHP output before
the headers breaks FastCGI responses. Errors go to `config/logs/php_error.log`.

### Apache and ports

- Server name, connection limits, keep-alive, Gzip, optional Brotli and browser caching for
  static assets, under **Settings › Apache**.
- HTTPS with your own certificate and key.
- **Ports** window:
  - **Apache ports:** primary HTTP / HTTPS and any number of extra ports. Each row says
    whether the port is free, in use by Hamster Server, or taken by another program.
  - **System ports:** every TCP port listening on the computer, with its address, program
    and PID. Hamster Server's ports are green; a program holding a port Hamster Server needs
    is red, with a warning on the tab. Well-known services (MySQL, Redis, Vite, Xdebug…) are
    named. Filter, sort, refresh with F5, and right-click to open the site, open the
    program's folder or copy the row.
- Custom Apache directives are checked with `httpd -t` before they are saved, so a typo
  cannot leave Apache unable to start.
- Apache listens on `127.0.0.1` only unless you allow LAN access.

### Virtual hosts

- Presets for Laravel, Symfony, CodeIgniter, Yii, CakePHP, Phalcon, Hyperf (reverse proxy),
  Slim, WordPress, Drupal and Magento. Each preset finds the public web root, routes through
  the front controller and blocks sensitive files.
- Enable or disable a site without deleting it, open it in the browser, or open its project
  folder, from the toolbar or the right-click menu.
- Hosts-file check: an enabled site whose name does not point at `127.0.0.1` is shown in amber,
  and the window gives you the line to add. Names ending in `.localhost` work without an entry.

### Interface

- A native Windows Forms interface with system colours, sized for high-DPI displays.
- One SVG icon set drawn by the app at the exact size your display needs, with colours that
  mean the same thing everywhere: green starts or runs, red stops or removes, amber is
  in-between or worth a look, blue is a neutral action. See
  [`doc/ICONS_AND_LOGO.md`](doc/ICONS_AND_LOGO.md).

## Bundled runtimes

| Component | Bundled version |
|---|---|
| Apache HTTP Server | 2.4.68 VS18 Win64 |
| PHP | 8.2.33 NTS VS16 x64 |
| PHP | 8.3.33 NTS VS16 x64 |
| PHP | 8.4.24 NTS VS17 x64 |
| PHP | 8.5.9 NTS VS17 x64 |
| CA certificate bundle | Mozilla roots extracted 2026-08-13 |

With automatic updates on, the active branch moves to the newest php.net patch after the first
start. Checksums and upstream archives of the bundled versions are recorded in
[`THIRD_PARTY_RUNTIMES.md`](THIRD_PARTY_RUNTIMES.md).

## Requirements

- Windows x64.
- Git LFS when cloning or updating the bundled Apache/PHP binaries.
- .NET 10 SDK `10.0.400` or a compatible patch release for development.
- .NET 10 Desktop Runtime to run a framework-dependent build.
- Internet access to php.net for PHP downloads and updates. Without it, Hamster Server works
  with the runtimes it already has.

## Getting started

```powershell
git lfs install
dotnet restore HamsterServer.slnx
dotnet build HamsterServer.slnx
dotnet run --project HamsterServer/HamsterServer.csproj
```

After the window opens:

1. Pick a PHP version in the main window, or open **Manage…** to install another one.
2. Review **Settings** (Apache, PHP, General) and **PHP Configuration**.
3. Choose **Start All**.
4. Open `http://localhost/`, or add a site under **Virtual Hosts**.

## Troubleshooting

| Symptom | Where to look |
|---|---|
| Apache or PHP will not start: "port already in use" | **Ports › System ports** shows the program holding the port. Stop it, or move Apache to another port under **Apache ports**. |
| A virtual host opens nothing in the browser | **Virtual Hosts**: an amber "not in hosts file" status means the name must be added to `C:\Windows\System32\drivers\etc\hosts` (the footer shows the line). Or use a `.localhost` name. |
| An extension disappeared after switching PHP version | **PHP Version Manager** lists missing extensions per runtime. Within a branch, **Copy extensions** brings them back; for another branch, download the DLL built for that PHP version (NTS x64). |
| A PHP update did not install | The log pane shows the reason, prefixed with `[PHP]`. **Check for updates** in the PHP Version Manager retries. |

## Validate and publish

```powershell
dotnet test HamsterServer.slnx
dotnet format HamsterServer.slnx --verify-no-changes
dotnet publish HamsterServer/HamsterServer.csproj -c Release
```

Publishing copies Apache, PHP, web content, configuration templates and the `ssl/` runtime
assets into the output folder.

Some tests only run when asked, because they reach outside the test process:

| Variable | Runs |
|---|---|
| `HAMSTER_SOAK=1` with `--filter "TestCategory=Soak"` | load tests against the real stack. They bind ports and stop every Apache/PHP process of the build, so close Hamster Server first. |
| `HAMSTER_LIVE_PHPNET=1` | parses the live php.net release catalog. |
| `HAMSTER_LIVE_INSTALL=1` (with the above) | downloads, installs and runs the newest release in a temporary folder. |
| `HAMSTER_FORM_SNAPSHOT_DIR=<folder>` | renders the configuration dialogs to PNG files. |
| `HAMSTER_GLYPH_SHEET=<file>.png` | renders the whole icon set to one sheet. |

## Repository layout

| Path | Purpose |
|---|---|
| `HamsterServer/Core/` | Service lifecycle, configuration generation, PHP updates, port scanning, logging |
| `HamsterServer/Models/` | Persisted settings, ports, virtual hosts and framework profiles |
| `HamsterServer/Forms/` | Secondary WinForms windows |
| `HamsterServer/apache/` | Bundled Apache runtime |
| `HamsterServer/php/` | Versioned PHP runtimes; the folder name is the version |
| `HamsterServer/config/` | Runtime configuration and generated files |
| `HamsterServer/www/` | Default document root, also the benchmark dashboard |
| `ssl/` | CA bundle and OpenSSL runtime assets |
| `doc/` | Performance measurements and the icon and logo reference |
| `HamsterServer.Tests/` | MSTest unit, runtime smoke, soak and opt-in live tests |

## Configuration and security

Settings are stored in `config/hamster-server.json` and saved atomically with a backup.
`httpd.conf`, the virtual-host files and every runtime's `php.ini` are generated from them,
so edits to the generated files are replaced. Do not commit machine-specific configuration,
logs, PID files, private keys or certificates.

On first launch after upgrading from XServer, an existing `config/xserver.json` and its backup
are copied to the Hamster Server file names automatically.

PHP downloads are accepted only from `windows.php.net` / `downloads.php.net` over HTTPS and
only with a matching SHA-256. Hamster Server reads the Windows hosts file but never writes it.
Apache listens on `127.0.0.1` by default; enable LAN access only on a trusted network and
review Windows Firewall rules first.

## Development workflow

- `main` is the stable branch; work happens on `dev` and reaches `main` through pull requests.
- Keep commits focused and add tests for reusable configuration or lifecycle logic.
- Include screenshots in pull requests for visible UI changes.
- [`CLAUDE.md`](CLAUDE.md) and [`AGENTS.md`](AGENTS.md) describe the architecture and the
  conventions the code relies on.

## Contributing and license

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) and the
[Code of Conduct](CODE_OF_CONDUCT.md) before opening an issue or pull request. Report security
vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

Hamster Server source code is available under the [MIT License](LICENSE). Bundled Apache, PHP
and related runtime components keep their upstream licenses; see
[THIRD_PARTY_RUNTIMES.md](THIRD_PARTY_RUNTIMES.md).

## Sponsorship

If Hamster Server helps your development workflow, you can support its maintenance through
[GitHub Sponsors](https://github.com/sponsors/cmm-cmm).
