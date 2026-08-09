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
            WorkingDirectory =
                workingDirectory ?? Environment.CurrentDirectory,

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
            {
                output.AppendLine(e.Data);
            }

            outputReceived?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;

            lock (output)
            {
                output.AppendLine(e.Data);
            }

            outputReceived?.Invoke(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                return new ProcessResult(
                    -1,
                    "Process could not be started.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(
                cancellationToken);

            return new ProcessResult(
                process.ExitCode,
                output.ToString());
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

            return new ProcessResult(
                -1,
                "Process stopped.");
        }
        catch (Exception ex)
        {
            return new ProcessResult(
                -1,
                ex.Message);
        }
    }

    public bool TryStartTerminal(
        string workingDirectory)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true
                    });

                return true;
            }

            var terminals = new[]
            {
                "kitty",
                "konsole",
                "gnome-terminal",
                "xfce4-terminal",
                "x-terminal-emulator"
            };

            foreach (var terminal in terminals)
            {
                try
                {
                    Process.Start(
                        new ProcessStartInfo
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
}

public sealed record ProcessResult(
    int ExitCode,
    string Output);