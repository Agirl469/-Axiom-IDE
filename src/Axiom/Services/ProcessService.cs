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
                workingDirectory
                ?? Environment.CurrentDirectory,

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

        if (!process.Start())
        {
            return new ProcessResult(
                -1,
                "Process could not be started.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(
                cancellationToken);
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

            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            output.ToString());
    }

    public bool TryStartTerminal(string command)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/k {command}",
                        UseShellExecute = true
                    });

                return true;
            }

            var terminals = new[]
            {
                "x-terminal-emulator",
                "kitty",
                "konsole",
                "gnome-terminal"
            };

            foreach (var terminal in terminals)
            {
                try
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = terminal,
                            Arguments =
                                $"-e sh -c \"{command}; exec $SHELL\"",

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