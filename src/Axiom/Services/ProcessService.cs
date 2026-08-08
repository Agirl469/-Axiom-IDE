using System.Diagnostics;

namespace Axiom.Services;

public sealed class ProcessService
{
    public async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = (await stdout) + (await stderr);
        return (process.ExitCode, output.Trim());
    }

    public bool TryStartTerminal(string command)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -Command \"{command.Replace("\"", "`\"")}\"",
                    UseShellExecute = true
                });
                return true;
            }

            var terminals = new[]
            {
                ("kitty", $"bash -lc '{EscapeShell(command)}; exec bash'"),
                ("konsole", $"-e bash -lc '{EscapeShell(command)}; exec bash'"),
                ("gnome-terminal", $"-- bash -lc '{EscapeShell(command)}; exec bash'"),
                ("xfce4-terminal", $"-e \"bash -lc '{EscapeShell(command)}; exec bash'\"")
            };

            foreach (var (terminal, args) in terminals)
            {
                if (!CommandExists(terminal))
                    continue;

                Process.Start(new ProcessStartInfo
                {
                    FileName = terminal,
                    Arguments = args,
                    UseShellExecute = false
                });
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool CommandExists(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.Split(Path.PathSeparator)
            .Select(directory => Path.Combine(directory, command))
            .Any(File.Exists);
    }

    private static string EscapeShell(string text)
    {
        return text.Replace("'", "'\\''");
    }
}
