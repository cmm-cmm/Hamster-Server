# Third-Party Runtime Inventory

Hamster Server bundles 64-bit, non-thread-safe PHP builds and Apache HTTP Server for Windows. Update these artifacts as complete directories; do not mix DLLs between patch versions. Verify every downloaded archive before replacing a runtime.

Executable runtime artifacts are tracked with Git LFS. Install Git LFS before cloning or committing runtime updates; text licenses and configuration templates remain normal Git files.

| Runtime | Source archive | SHA-256 |
|---|---|---|
| Apache 2.4.68 VS18 Win64 | [Apache Lounge](https://www.apachelounge.com/download/VS18/binaries/httpd-2.4.68-260617-Win64-VS18.zip) | `eac38a9f8d21b6ba0817cd1576a3d291cc771afd3c6a210d120bdea6f4aa4aeb` |
| PHP 8.2.35-1 NTS VS16 x64 | [PHP for Windows](https://downloads.php.net/~windows/releases/php-8.2.35-1-nts-Win32-vs16-x64.zip) | `ebe75134c03869826cb1714d31fd5eb96afefa6d9fd35d786971c03020e7bce2` |
| PHP 8.3.36-1 NTS VS16 x64 | [PHP for Windows](https://downloads.php.net/~windows/releases/php-8.3.36-1-nts-Win32-vs16-x64.zip) | `1a097d52b7a68622cdc723073b61cc70caf79c941218ff65b2d8441dbc6b10d4` |
| PHP 8.4.27-1 NTS VS17 x64 | [PHP for Windows](https://downloads.php.net/~windows/releases/php-8.4.27-1-nts-Win32-vs17-x64.zip) | `dbae154a0a89791f36c3d48e68bae797878333e7d6cb5721e74617de59b63ab7` |
| PHP 8.5.12-1 NTS VS17 x64 | [PHP for Windows](https://downloads.php.net/~windows/releases/php-8.5.12-1-nts-Win32-vs17-x64.zip) | `2a36a2edcb7a5b9252f638fd5e4cf84d7f6e066092654220dc0b63e5cf4e9723` |

These builds contain the fixes for CVE-2026-109618 and CVE-2026-109619 ("PHP2Shell", unauthenticated remote code execution, fixed in 8.2.35, 8.3.36, 8.4.27 and 8.5.12). The `-1` suffix is the windows.php.net rebuild of that release; folders keep the catalog's name verbatim. Never bundle a patch below those versions.

PHP 8.2 receives security fixes only through 31 December 2026. Remove that branch after its support window unless compatibility requirements explicitly justify retaining it.

## TLS assets

The `ssl/` directory is copied into build and publish output. Hamster Server uses its CA
bundle for PHP cURL and OpenSSL peer verification when the corresponding PHP
setting is enabled (the default).

| Asset | Source/version | SHA-256 |
|---|---|---|
| `cacert.pem` | [curl CA Extract](https://curl.se/docs/caextract.html), Mozilla roots extracted 2026-08-13 | `f66dff1bdf8f96060b8177976f8b7d9254bc89bc4db933d769f7384d28480bc9` |
| `legacy.dll` | [OpenSSL](https://www.openssl.org/) legacy provider 3.0.8 | `8ab0434565c24ee9782f82d9992226274f361f46af4df8b6f16e836724ab355a` |
| `openssl.cnf` | OpenSSL configuration | `a41adad50f74ecfdd9209cfe2c31f3be2abe4c15ce92c958c063c97c14c13b81` |
