using System.Diagnostics;
using System.Text;

namespace Axiom.Services;

public sealed class ProcessService
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? outputReceived = null,
        CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;

            lock (output)
                output.AppendLine(e.Data);

            outputReceived?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;

            lock (output)
                output.AppendLine(e.Data);

            outputReceived?.Invoke(e.Data);
        };

        try
        {
            if (!process.Start())
                return new ProcessResult(-1, "Process could not be started.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new ProcessResult(process.ExitCode, output.ToString());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return new ProcessResult(-1, "Process stopped.");
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, ex.Message);
        }
    }

    public bool TryStartTerminal(string workingDirectory)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                });

                return true;
            }

            foreach (var terminal in GetLinuxTerminals())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = terminal,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false
                    });

                    return true;
                }
                catch
                {
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool TryRunCommandInTerminal(
        string command,
        string? workingDirectory = null)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -Command \"{EscapePowerShell(command)}\"",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                });

                return true;
            }

            var shellCommand =
                $"cd {QuoteShell(workingDirectory)} && {command}; " +
                "printf '\\nPress Enter to close...'; read _";

            var terminalCommands = new (string FileName, string Arguments)[]
            {
                ("kitty", $"sh -lc {QuoteArgument(shellCommand)}"),
                ("konsole", $"-e sh -lc {QuoteArgument(shellCommand)}"),
                ("gnome-terminal", $"-- sh -lc {QuoteArgument(shellCommand)}"),
                ("xfce4-terminal", $"--command=\"sh -lc {EscapeDoubleQuoted(QuoteArgument(shellCommand))}\""),
                ("x-terminal-emulator", $"-e sh -lc {QuoteArgument(shellCommand)}")
            };

            foreach (var terminal in terminalCommands)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = terminal.FileName,
                        Arguments = terminal.Arguments,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false
                    });

                    return true;
                }
                catch
                {
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string[] GetLinuxTerminals() =>
    [
        "kitty",
        "konsole",
        "gnome-terminal",
        "xfce4-terminal",
        "x-terminal-emulator"
    ];

    private static string EscapePowerShell(string value) =>
        value.Replace("`", "``").Replace("\"", "`\"");

    private static string QuoteShell(string value) =>
        "'" + value.Replace("'", "'\\''") + "'";

    private static string QuoteArgument(string value) =>
        "'" + value.Replace("'", "'\\''") + "'";

    private static string EscapeDoubleQuoted(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

public sealed record ProcessResult(
    int ExitCode,
    string Output);
