
using Axiom.Models;

namespace Axiom.Services;

public sealed class ToolchainService
{
    private readonly ProcessService _process = new();
    private readonly PlatformService _platform = new();

    public IReadOnlyList<ToolchainInfo> Toolchains { get; } = new List<ToolchainInfo>
    {
        new()
        {
            Name = ".NET SDK",
            Description = "C#, F#, MSBuild and the dotnet CLI.",
            Command = "dotnet",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "dotnet-sdk-10.0",
                ["dnf"] = "dotnet-sdk-10.0",
                ["pacman"] = "dotnet-sdk"
            },
            WingetId = "Microsoft.DotNet.SDK.10"
        },
        new()
        {
            Name = "GCC / G++",
            Description = "GNU C and C++ compiler toolchain.",
            Command = "g++",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "build-essential",
                ["dnf"] = "gcc gcc-c++",
                ["pacman"] = "base-devel gcc"
            },
            WingetId = "BrechtSanders.WinLibs.POSIX.UCRT"
        },
        new()
        {
            Name = "Clang / LLVM",
            Description = "Clang compiler and LLVM development tools.",
            Command = "clang",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "clang lldb",
                ["dnf"] = "clang lldb",
                ["pacman"] = "clang lldb"
            },
            WingetId = "LLVM.LLVM"
        },
        new()
        {
            Name = "CMake",
            Description = "Project configuration and build generation for C and C++.",
            Command = "cmake",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "cmake",
                ["dnf"] = "cmake",
                ["pacman"] = "cmake"
            },
            WingetId = "Kitware.CMake"
        },
        new()
        {
            Name = "Ninja",
            Description = "Small and fast build executor, useful with CMake.",
            Command = "ninja",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "ninja-build",
                ["dnf"] = "ninja-build",
                ["pacman"] = "ninja"
            },
            WingetId = "Ninja-build.Ninja"
        },
        new()
        {
            Name = "Rust",
            Description = "Rust compiler and Cargo package/build tool.",
            Command = "rustc",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "rustc cargo",
                ["dnf"] = "rust cargo",
                ["pacman"] = "rustup"
            },
            WingetId = "Rustlang.Rustup"
        },
        new()
        {
            Name = "Java JDK",
            Description = "Java compiler and runtime for JVM projects.",
            Command = "javac",
            ProbeCommands = ["-version"],
            LinuxPackages = new()
            {
                ["apt"] = "default-jdk",
                ["dnf"] = "java-latest-openjdk-devel",
                ["pacman"] = "jdk-openjdk"
            },
            WingetId = "EclipseAdoptium.Temurin.25.JDK"
        },
        new()
        {
            Name = "Python",
            Description = "Python interpreter and package tooling.",
            Command = OperatingSystem.IsWindows() ? "python" : "python3",
            ProbeCommands = ["--version"],
            LinuxPackages = new()
            {
                ["apt"] = "python3 python3-pip",
                ["dnf"] = "python3 python3-pip",
                ["pacman"] = "python python-pip"
            },
            WingetId = "Python.Python.3.13"
        }
    };

    public async Task<List<ToolchainStatus>> ScanAsync()
    {
        var result = new List<ToolchainStatus>();

        foreach (var toolchain in Toolchains)
        {
            var status = await ProbeAsync(toolchain);
            result.Add(status);
        }

        return result;
    }

    public async Task<ToolchainStatus> ProbeAsync(ToolchainInfo toolchain)
    {
        try
        {
            var arguments = string.Join(' ', toolchain.ProbeCommands);
            var result = await _process.RunAsync(toolchain.Command, arguments);
            if (result.ExitCode == 0)
            {
                var version = result.Output
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?
                    .Trim()
                    ?? "Installed";

                return new ToolchainStatus
                {
                    Toolchain = toolchain,
                    Installed = true,
                    Version = version,
                    InstallCommand = BuildInstallCommand(toolchain)
                };
            }
        }
        catch
        {
        }

        return new ToolchainStatus
        {
            Toolchain = toolchain,
            Installed = false,
            InstallCommand = BuildInstallCommand(toolchain)
        };
    }

    public string BuildInstallCommand(ToolchainInfo toolchain)
    {
        if (OperatingSystem.IsWindows())
        {
            return toolchain.WingetId is null
                ? "No winget package configured"
                : $"winget install --id {toolchain.WingetId} -e";
        }

        var manager = _platform.LinuxPackageManager;
        if (!toolchain.LinuxPackages.TryGetValue(manager, out var package))
            return "Package manager not supported yet";

        return manager switch
        {
            "apt" => $"sudo apt update && sudo apt install -y {package}",
            "pacman" => $"sudo pacman -S --needed {package}",
            "dnf" => $"sudo dnf install -y {package}",
            "zypper" => $"sudo zypper install {package}",
            _ => "Package manager not supported yet"
        };
    }
}
