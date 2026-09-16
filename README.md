# Hamster Server

Hamster Server is a Windows-only local development server that bundles Apache HTTP Server,
multiple PHP runtimes, and a native WinForms control panel. It provides a portable
environment for running PHP applications without installing or configuring each
service separately.

The current development source identifies as **Hamster Server 1.2.0** and targets
**.NET 10 for Windows**.

Hamster Server is a product of [Konex Forge](https://konexforge.com/).

## Features

- Start, stop, and restart Apache and PHP-CGI together or independently.
- Switch between bundled PHP versions without reinstalling the application.
- Download, update, and remove PHP for Windows x64 NTS runtimes from the built-in
  **PHP Version Manager**. Downloads are verified against php.net SHA-256 checksums
  and extracted through a staging directory before becoming available.
- Monitor process health and automatically restart PHP-CGI after unexpected exits.
- Configure FastCGI workers, limits, OPcache, time zone, and PHP extensions.
  The recommended XAMPP-compatible profile is verified across every bundled PHP
  version and is synchronized to all generated `php.ini` files when saved.
- Manage HTTP, HTTPS, and additional listening ports from the UI.
- Create virtual hosts with presets for Laravel, Symfony, CodeIgniter, Yii,
  CakePHP, Phalcon, Hyperf, Slim, WordPress, Drupal, and Magento.
- Enable Gzip, browser caching, and optional Brotli compression.
- Configure Apache server certificates for local HTTPS sites.
- Verify outbound PHP HTTPS/API requests with the bundled Mozilla CA store. This
  option is enabled by default and writes absolute `curl.cainfo` and
  `openssl.cafile` paths into the generated `php.ini`.
- Keep Apache and PHP logs accessible from the control panel.
- Minimize to the system tray, start services automatically, or launch with Windows.
- Bind to loopback by default, with explicit opt-in for LAN access.

## Bundled runtimes

| Component | Bundled version |
|---|---|
| Apache HTTP Server | 2.4.68 VS18 Win64 |
| PHP | 8.2.33 NTS VS16 x64 |
| PHP | 8.3.33 NTS VS16 x64 |
| PHP | 8.4.24 NTS VS17 x64 |
| PHP | 8.5.9 NTS VS17 x64 |
| CA certificate bundle | Mozilla roots extracted 2026-08-13 |

Checksums and upstream archive details are recorded in
[`THIRD_PARTY_RUNTIMES.md`](THIRD_PARTY_RUNTIMES.md).

## Requirements

- Windows x64.
- Git LFS when cloning or updating bundled Apache/PHP binaries.
- .NET 10 SDK `10.0.400` or a compatible patch release for development.
- .NET 10 Desktop Runtime when running a framework-dependent publish.

## Build and run

```powershell
git lfs install
dotnet restore HamsterServer.slnx
dotnet build HamsterServer.slnx
dotnet run --project HamsterServer/HamsterServer.csproj
```

After the application opens:

1. Select a PHP version from the main window, or choose **Manage…** to download an
   official release. Updates install the latest patch side-by-side for safe rollback;
   switch to it before deleting the previous version.
2. Review Apache and PHP options under **Settings**.
3. Choose **Start All**.
4. Open `http://localhost/` or one of the configured virtual hosts.

The bundled CA store used by PHP API requests can be controlled under
**Settings > PHP > Runtime > HTTPS CA certificate**. Apache server-side HTTPS uses
the separate certificate and private-key fields under **Settings > Apache**.

## Validate and publish

```powershell
dotnet test HamsterServer.slnx
dotnet format HamsterServer.slnx --verify-no-changes
dotnet publish HamsterServer/HamsterServer.csproj -c Release
```

Publishing copies Apache, PHP, web content, generated-configuration templates, and
the `ssl/` runtime assets into the application output.

## Repository layout

| Path | Purpose |
|---|---|
| `HamsterServer/Core/` | Service lifecycle, configuration generation, logging, and runtime discovery |
| `HamsterServer/Models/` | Persisted settings, ports, virtual hosts, and framework profiles |
| `HamsterServer/Forms/` | Secondary WinForms configuration screens |
| `HamsterServer/apache/` | Bundled Apache runtime |
| `HamsterServer/php/` | Versioned PHP runtimes |
| `HamsterServer/config/` | Runtime configuration and generated files |
| `HamsterServer/www/` | Default document root |
| `ssl/` | CA bundle and OpenSSL runtime assets |
| `HamsterServer.Tests/` | MSTest unit and runtime smoke tests |

## Configuration and security

Application settings are stored in `config/hamster-server.json`. Apache configuration,
virtual-host files, and each selected PHP runtime's `php.ini` are generated from
those settings. Do not commit machine-specific configuration, logs, PID files,
private keys, or certificates.

On first launch after upgrading from XServer, an existing `config/xserver.json`
and its backup are copied to the new Hamster Server filenames automatically.

Apache listens on `127.0.0.1` by default. Enable LAN access only on a trusted
network and review Windows Firewall rules before exposing a development server.

## Development workflow

- `main` is the stable branch.
- Create focused feature or fix branches from `main`.
- Keep commits focused and include tests for reusable configuration or lifecycle logic.
- Include screenshots in pull requests for visible UI changes.

## Contributing and License

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) and the
[Code of Conduct](CODE_OF_CONDUCT.md) before opening an issue or pull request.
Security vulnerabilities must be reported privately according to
[SECURITY.md](SECURITY.md).

Hamster Server source code is available under the [MIT License](LICENSE).
Bundled Apache, PHP, and related runtime components retain their respective
upstream licenses; see [THIRD_PARTY_RUNTIMES.md](THIRD_PARTY_RUNTIMES.md).

## Sponsorship

If Hamster Server helps your development workflow, you can support its ongoing
maintenance through [GitHub Sponsors](https://github.com/sponsors/cmm-cmm).
