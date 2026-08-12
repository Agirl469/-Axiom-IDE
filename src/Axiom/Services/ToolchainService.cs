using Axiom.Models;

namespace Axiom.Services;

public sealed class ToolchainService
{
    private readonly ProcessService _process =
        new();

    private readonly PlatformService _platform =
        new();

    public IReadOnlyList<ToolchainBundle> Bundles { get; } =
    [
        new()
        {
            Id = "cpp",
            Name = "C / C++ Development",
            ShortName = "C++",
            Description =
                "Compiler, CMake and Ninja for normal C and C++ development.",

            ToolchainIds =
            [
                "gcc",
                "cmake",
                "ninja"
            ]
        },

        new()
        {
            Id = "dotnet-dev",
            Name = ".NET Development",
            ShortName = ".NET",
            Description =
                "The .NET SDK for C#, F# and .NET projects.",

            ToolchainIds =
            [
                "dotnet"
            ]
        },

        new()
        {
            Id = "python-dev",
            Name = "Python Development",
            ShortName = "PY",
            Description =
                "Python and pip for scripts and Python projects.",

            ToolchainIds =
            [
                "python"
            ]
        },

        new()
        {
            Id = "rust-dev",
            Name = "Rust Development",
            ShortName = "RS",
            Description =
                "Rust compiler and Cargo toolchain.",

            ToolchainIds =
            [
                "rust"
            ]
        },

        new()
        {
            Id = "web",
            Name = "Web Development",
            ShortName = "WEB",
            Description =
                "Node.js and npm for JavaScript and web projects.",

            ToolchainIds =
            [
                "node"
            ]
        },

        new()
        {
            Id = "lua-dev",
            Name = "Lua Development",
            ShortName = "LUA",
            Description =
                "Lua runtime for Lua projects and scripts.",

            ToolchainIds =
            [
                "lua"
            ]
        }
    ];

    public IReadOnlyList<ToolchainInfo> Toolchains { get; } =
    [
        new()
        {
            Id = "dotnet",

            Name = ".NET SDK",

            ShortName = ".NET",

            Category = "SDK",

            Description =
                "C#, F#, MSBuild and the dotnet CLI.",

            Command = "dotnet",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "dotnet-sdk-10.0",

                ["dnf"] =
                    "dotnet-sdk-10.0",

                ["pacman"] =
                    "dotnet-sdk"
            },

            WingetId =
                "Microsoft.DotNet.SDK.10"
        },

        new()
        {
            Id = "gcc",

            Name = "GCC / G++",

            ShortName = "GCC",

            Category = "Compiler",

            Description =
                "GNU C and C++ compiler toolchain.",

            Command = "g++",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "build-essential",

                ["dnf"] =
                    "gcc gcc-c++",

                ["pacman"] =
                    "base-devel gcc"
            },

            WingetId =
                "BrechtSanders.WinLibs.POSIX.UCRT"
        },

        new()
        {
            Id = "clang",

            Name = "Clang / LLVM",

            ShortName = "CL",

            Category = "Compiler",

            Description =
                "Clang compiler and LLVM development tools.",

            Command = "clang",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "clang lldb",

                ["dnf"] =
                    "clang lldb",

                ["pacman"] =
                    "clang lldb"
            },

            WingetId =
                "LLVM.LLVM"
        },

        new()
        {
            Id = "cmake",

            Name = "CMake",

            ShortName = "CM",

            Category = "Build Tool",

            Description =
                "Project configuration and build generation for C and C++.",

            Command = "cmake",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "cmake",

                ["dnf"] =
                    "cmake",

                ["pacman"] =
                    "cmake"
            },

            WingetId =
                "Kitware.CMake"
        },

        new()
        {
            Id = "ninja",

            Name = "Ninja",

            ShortName = "NJ",

            Category = "Build Tool",

            Description =
                "Fast build executor commonly used with CMake.",

            Command = "ninja",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "ninja-build",

                ["dnf"] =
                    "ninja-build",

                ["pacman"] =
                    "ninja"
            },

            WingetId =
                "Ninja-build.Ninja"
        },

        new()
        {
            Id = "rust",

            Name = "Rust",

            ShortName = "RS",

            Category = "Compiler",

            Description =
                "Rust compiler and Cargo package/build tool.",

            Command = "rustc",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "rustc cargo",

                ["dnf"] =
                    "rust cargo",

                ["pacman"] =
                    "rustup"
            },

            WingetId =
                "Rustlang.Rustup"
        },

        new()
        {
            Id = "java",

            Name = "Java JDK",

            ShortName = "JDK",

            Category = "SDK",

            Description =
                "Java compiler and runtime for JVM projects.",

            Command = "javac",

            ProbeCommands =
            [
                "-version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "default-jdk",

                ["dnf"] =
                    "java-latest-openjdk-devel",

                ["pacman"] =
                    "jdk-openjdk"
            },

            WingetId =
                "EclipseAdoptium.Temurin.25.JDK"
        },

        new()
        {
            Id = "python",

            Name = "Python",

            ShortName = "PY",

            Category = "Runtime",

            Description =
                "Python interpreter and package tooling.",

            Command =
                OperatingSystem.IsWindows()
                    ? "python"
                    : "python3",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "python3 python3-pip",

                ["dnf"] =
                    "python3 python3-pip",

                ["pacman"] =
                    "python python-pip"
            },

            WingetId =
                "Python.Python.3.13"
        },

        new()
        {
            Id = "node",

            Name = "Node.js",

            ShortName = "JS",

            Category = "Runtime",

            Description =
                "JavaScript runtime with npm package tooling.",

            Command = "node",

            ProbeCommands =
            [
                "--version"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "nodejs npm",

                ["dnf"] =
                    "nodejs npm",

                ["pacman"] =
                    "nodejs npm"
            },

            WingetId =
                "OpenJS.NodeJS.LTS"
        },

        new()
        {
            Id = "lua",

            Name = "Lua",

            ShortName = "LUA",

            Category = "Runtime",

            Description =
                "Lua interpreter for Lua projects and scripts.",

            Command = "lua",

            ProbeCommands =
            [
                "-v"
            ],

            LinuxPackages = new()
            {
                ["apt"] =
                    "lua5.4",

                ["dnf"] =
                    "lua",

                ["pacman"] =
                    "lua"
            }
        }
    ];

    public async Task<List<ToolchainStatus>> ScanAsync()
    {
        var tasks =
            Toolchains.Select(
                ProbeAsync);

        return (
            await Task.WhenAll(tasks)
        ).ToList();
    }

    public async Task<ToolchainStatus> ProbeAsync(
        ToolchainInfo toolchain)
    {
        try
        {
            var arguments =
                string.Join(
                    ' ',
                    toolchain.ProbeCommands);

            var result =
                await _process.RunAsync(
                    toolchain.Command,
                    arguments);

            if (result.ExitCode == 0)
            {
                var version =
                    result.Output
                        .Split(
                            '\n',
                            StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?
                        .Trim()
                    ?? "Installed";

                return new ToolchainStatus
                {
                    Toolchain =
                        toolchain,

                    Installed =
                        true,

                    Version =
                        ShortenVersion(
                            version),

                    InstallCommand =
                        BuildInstallCommand(
                            toolchain)
                };
            }
        }
        catch
        {
        }

        return new ToolchainStatus
        {
            Toolchain =
                toolchain,

            Installed =
                false,

            InstallCommand =
                BuildInstallCommand(
                    toolchain)
        };
    }

    public List<ToolchainStatus> GetBundleStatuses(
        ToolchainBundle bundle,
        IEnumerable<ToolchainStatus> statuses)
    {
        var ids =
            new HashSet<string>(
                bundle.ToolchainIds,
                StringComparer.OrdinalIgnoreCase);

        return statuses
            .Where(
                status =>
                    ids.Contains(
                        status.Toolchain.Id))
            .ToList();
    }

    public string BuildInstallCommand(
        ToolchainInfo toolchain)
    {
        if (OperatingSystem.IsWindows())
        {
            return string.IsNullOrWhiteSpace(
                toolchain.WingetId)
                ? string.Empty
                : $"winget install --id {toolchain.WingetId} -e " +
                  "--accept-source-agreements " +
                  "--accept-package-agreements";
        }

        var manager =
            _platform.LinuxPackageManager;

        if (!toolchain.LinuxPackages.TryGetValue(
                manager,
                out var package))
        {
            return string.Empty;
        }

        return manager switch
        {
            "apt" =>
                $"sudo apt update && sudo apt install -y {package}",

            "pacman" =>
                $"sudo pacman -S --needed {package}",

            "dnf" =>
                $"sudo dnf install -y {package}",

            "zypper" =>
                $"sudo zypper install -y {package}",

            _ =>
                string.Empty
        };
    }

    public string BuildBundleInstallCommand(
        ToolchainBundle bundle,
        IEnumerable<ToolchainStatus> statuses)
    {
        var missing =
            GetBundleStatuses(
                    bundle,
                    statuses)
                .Where(
                    status =>
                        !status.Installed)
                .ToList();

        var commands =
            missing
                .Select(
                    status =>
                        status.InstallCommand)
                .Where(
                    command =>
                        !string.IsNullOrWhiteSpace(
                            command))
                .Distinct()
                .ToList();

        return string.Join(
            " && ",
            commands);
    }

    public async Task<ProcessResult> InstallOnWindowsAsync(
        ToolchainInfo toolchain,
        Action<string>? output = null,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ProcessResult(
                -1,
                "Windows installer called on a non-Windows platform.");
        }

        if (string.IsNullOrWhiteSpace(
                toolchain.WingetId))
        {
            return new ProcessResult(
                -1,
                "No automatic Windows package is configured for this toolchain.");
        }

        var arguments =
            $"install --id {toolchain.WingetId} -e " +
            "--accept-source-agreements " +
            "--accept-package-agreements";

        return await _process.RunAsync(
            "winget",
            arguments,
            outputReceived:
                output,
            cancellationToken:
                cancellationToken);
    }

    public async Task<ProcessResult> InstallBundleOnWindowsAsync(
        ToolchainBundle bundle,
        IEnumerable<ToolchainStatus> statuses,
        Action<string>? output = null,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ProcessResult(
                -1,
                "Windows installer called on a non-Windows platform.");
        }

        var missing =
            GetBundleStatuses(
                    bundle,
                    statuses)
                .Where(
                    status =>
                        !status.Installed)
                .ToList();

        if (missing.Count == 0)
        {
            return new ProcessResult(
                0,
                "Everything in this development bundle is already installed.");
        }

        foreach (var status in missing)
        {
            if (string.IsNullOrWhiteSpace(
                    status.Toolchain.WingetId))
            {
                output?.Invoke(
                    $"Skipping {status.Toolchain.Name}: no automatic Windows installer.");

                continue;
            }

            output?.Invoke(
                $"Installing {status.Toolchain.Name}...");

            var result =
                await InstallOnWindowsAsync(
                    status.Toolchain,
                    output,
                    cancellationToken);

            if (result.ExitCode != 0)
                return result;
        }

        return new ProcessResult(
            0,
            "Development bundle installed.");
    }

    private static string ShortenVersion(
        string value)
    {
        value =
            value
                .Replace(
                    "\r",
                    string.Empty)
                .Trim();

        return value.Length <= 100
            ? value
            : value[..100] + "…";
    }
}