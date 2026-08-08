# Axiom

Axiom is a Linux-first, cross-platform IDE starter. Windows is supported by the same codebase, but Linux is the primary platform and the first place new platform features should be tested.

Axiom itself is built with C# and Avalonia. The repository includes `Axiom.sln` and `src/Axiom/Axiom.csproj` because those are convenient files for developing and building the IDE itself.

**Projects made with Axiom do not use `.sln` files.** Axiom projects use Axiom's own `.axn` project format.

## Axiom projects

A new C++ project named `hello` looks like this:

```text
hello/
├── hello.axn
└── src/
    └── main.cpp
```

`hello.axn`:

```json
{
  "format": 1,
  "name": "hello",
  "language": "cpp",
  "entry": "src/main.cpp",
  "sourceRoots": [
    "src"
  ],
  "settings": {
    "compiler": "g++",
    "standard": "c++20"
  }
}
```

The `.axn` file is the project. Later it can hold targets, dependencies, run profiles, build profiles, environment variables and extension-owned settings without adopting another IDE's project model.

## C# projects

An Axiom C# project still uses `.axn`:

```text
ConsoleApp/
├── ConsoleApp.axn
└── src/
    └── Program.cs
```

Axiom may generate temporary MSBuild metadata under `.axiom/dotnet/` when it invokes the real .NET toolchain. That generated file is build state and is not the user's project format.

## Current starter

- Native `.axn` projects
- New project templates for C++, C#, Rust and Python
- Open `.axn` project files directly
- Open plain folders in loose-file mode
- Basic file editor and build output
- GCC and Clang support
- .NET SDK support
- Rust/Cargo support
- Python support
- Toolchain detection and installer commands
- Linux package manager support for apt, pacman and dnf
- Windows winget support
- No AI features
- No account requirement
- No telemetry layer

## Build Axiom

Requirement: .NET 10 SDK.

Linux:

```bash
dotnet restore Axiom.sln
dotnet build Axiom.sln
dotnet run --project src/Axiom/Axiom.csproj
```

Windows PowerShell:

```powershell
dotnet restore .\Axiom.sln
dotnet build .\Axiom.sln
dotnet run --project .\src\Axiom\Axiom.csproj
```

The solution is only for developing Axiom. Axiom does not generate solutions for user projects.

## Internal build state

Axiom keeps generated build files out of the source layout where possible:

```text
.axiom/
├── build/
└── dotnet/
```

This directory can later also contain caches, indexes and language-server state.

## Next systems

1. Real editor tabs with dirty-file indicators.
2. Syntax highlighting and a proper editor component.
3. Debug and Release profiles stored in `.axn`.
4. Run profiles stored in `.axn`.
5. LSP support, beginning with clangd.
6. GDB/LLDB debugging through DAP.
7. A PTY-backed integrated terminal on Linux and Windows.
8. Git status, diff and commit UI.
9. Axiom toolchain registry.
10. Extension API after the core project model settles down.
