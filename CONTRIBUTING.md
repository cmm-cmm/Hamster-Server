# Contributing to Hamster Server

Thank you for helping improve Hamster Server. Keep changes focused, testable,
and compatible with Windows and .NET 10.

## Before You Start

- Search existing issues before opening a new one.
- Open an issue before large behavioral or architectural changes.
- Install Git LFS before cloning because Apache and PHP binaries use LFS.
- Never commit secrets, certificates, logs, PID files, or machine-specific configuration.

## Development Workflow

Create a branch from `main`, make a focused change, and run:

```powershell
dotnet restore HamsterServer.slnx
dotnet build HamsterServer.slnx
dotnet test HamsterServer.slnx
dotnet format HamsterServer.slnx --verify-no-changes
```

Add MSTest coverage under `HamsterServer.Tests/` for reusable logic. Manually
exercise affected WinForms workflows and include screenshots for visible UI changes.

## Pull Requests

Use a concise, imperative title. Explain the behavior change, link related issues,
and list validation performed. Call out changes to ports, generated configuration,
bundled runtimes, or Windows Registry behavior. By contributing, you agree that
your contribution is licensed under the MIT License.
