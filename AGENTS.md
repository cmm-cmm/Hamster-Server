# Repository Guidelines

## Project Structure & Module Organization

`HamsterServer.slnx` contains the Windows-only .NET 10 WinForms application in `HamsterServer/` and MSTest coverage in `HamsterServer.Tests/`. Application startup and the main window live in `Program.cs` and `HamsterServerForm*.cs`. Put service lifecycle and configuration logic in `Core/`, data objects in `Models/`, and secondary UI screens in `Forms/`. Runtime assets are bundled under `apache/`, versioned `php/` directories, `config/`, and `www/`; treat third-party binaries as vendor artifacts. WinForms `.Designer.cs` and `.resx` files should normally be changed through the designer and committed with their corresponding form code.

## Build, Test, and Development Commands

Run commands from the repository root on Windows with the .NET 10 SDK:

```powershell
dotnet restore HamsterServer.slnx
dotnet build HamsterServer.slnx
dotnet test HamsterServer.slnx
dotnet run --project HamsterServer/HamsterServer.csproj
dotnet publish HamsterServer/HamsterServer.csproj -c Release
dotnet format HamsterServer.slnx --verify-no-changes
```

Restore resolves dependencies; build compiles and copies Apache, PHP, configuration, and web content into the output. Test runs MSTest unit and runtime smoke tests. Run launches the desktop app. Publish creates an optimized distributable. The format check catches standard SDK style issues.

## Coding Style & Naming Conventions

Use four-space indentation, file-scoped namespaces, nullable annotations, and existing implicit usings. Name types, public members, and methods in `PascalCase`; locals, parameters, and private fields use `camelCase`, with `_camelCase` for fields. Keep asynchronous methods suffixed `Async` and pass `CancellationToken` where operations may be long-running. Keep UI event handling in forms and reusable process/configuration behavior in `Core/`.

## Testing Guidelines

Add automated coverage to `HamsterServer.Tests` for non-UI logic and use `*Tests.cs` filenames. Every change must pass `dotnet test`, `dotnet build`, and the formatter check. Also manually exercise affected UI workflows: application startup, Apache/PHP start-stop-restart, PHP version switching, settings persistence, and virtual-host changes.

## Commit & Pull Request Guidelines

Recent commits use concise, imperative summaries such as `Add PHP extension manager UI` and `Refactor Apache/PHP config handling`. Keep each commit focused and include generated form resources when applicable. Pull requests should explain the behavior change, list validation performed, link relevant issues, and include screenshots for visible UI changes. Call out changes to ports, generated Apache configuration, bundled runtime versions, or Windows Registry behavior.

## Security & Configuration Tips

Do not commit secrets, machine-specific paths, certificates, logs, PID files, or local runtime configuration. Review generated Apache/PHP configuration carefully and avoid unrelated edits to bundled executables and libraries.
