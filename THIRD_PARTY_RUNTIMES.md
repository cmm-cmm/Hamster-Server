# Third-Party Runtime Inventory

Hamster Server bundles 64-bit, non-thread-safe PHP builds and Apache HTTP Server for Windows. Update these artifacts as complete directories; do not mix DLLs between patch versions. Verify every downloaded archive before replacing a runtime.

Executable runtime artifacts are tracked with Git LFS. Install Git LFS before cloning or committing runtime updates; text licenses and configuration templates remain normal Git files.

| Runtime | Source archive | SHA-256 |
|---|---|---|
| Apache 2.4.68 VS18 Win64 | [Apache Lounge](https://www.apachelounge.com/download/VS18/binaries/httpd-2.4.68-260617-Win64-VS18.zip) | `eac38a9f8d21b6ba0817cd1576a3d291cc771afd3c6a210d120bdea6f4aa4aeb` |
| PHP 8.2.33 NTS VS16 x64 | [PHP for Windows](https://windows.php.net/downloads/releases/php-8.2.33-nts-Win32-vs16-x64.zip) | `d0bd189522fa50255ee94ed4b340ed4330f5ae33a90a74205275b0f0b221d388` |
| PHP 8.3.33 NTS VS16 x64 | [PHP for Windows](https://windows.php.net/downloads/releases/php-8.3.33-nts-Win32-vs16-x64.zip) | `534399107056313246f424adbbb7937337e40fbbf6aa7bc26287ba9cfd2e4a2a` |
| PHP 8.4.24 NTS VS17 x64 | [PHP for Windows](https://windows.php.net/downloads/releases/php-8.4.24-nts-Win32-vs17-x64.zip) | `86470a30cbbaeafb259e727dfa5cd336f2f3f0a462cd6f8e3eac00fdbded13cb` |
| PHP 8.5.9 NTS VS17 x64 | [PHP for Windows](https://windows.php.net/downloads/releases/php-8.5.9-nts-Win32-vs17-x64.zip) | `516c2d72231bd035c8a910120834add0ad208098b790b4909b2cbeb93ce135fc` |

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
