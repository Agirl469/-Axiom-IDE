using System.Text.RegularExpressions;

namespace Axiom.Build;

public static class BuildProblemParser
{
    // GCC / G++ / Clang
    //
    // Example:
    // src/main.cpp:12:5: error: expected ';' before '}'
    private static readonly Regex GccPattern =
        new(
            @"^(?<file>.+?):(?<line>\d+):(?<column>\d+):\s*(?<severity>fatal error|error|warning|note):\s*(?<message>.+)$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

    // MSBuild / dotnet
    //
    // Example:
    // C:\Project\Program.cs(12,5): error CS1002: ; expected [project.csproj]
    private static readonly Regex DotNetPattern =
        new(
            @"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s*(?<severity>error|warning)\s+(?<code>[A-Za-z]+\d+):\s*(?<message>.+?)(?:\s+\[.+\])?$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

    // Rust
    //
    // Cargo/rustc often prints a location on a separate line:
    //
    // --> src/main.rs:4:5
    //
    // For now we at least recognize simple inline forms.
    private static readonly Regex RustInlinePattern =
        new(
            @"^(?<file>.+?\.rs):(?<line>\d+):(?<column>\d+):\s*(?<severity>error|warning):\s*(?<message>.+)$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

    // Python traceback line
    //
    // File "src/main.py", line 12
    private static readonly Regex PythonLocationPattern =
        new(
            @"^\s*File\s+""(?<file>.+?)"",\s+line\s+(?<line>\d+)",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

    public static BuildProblem? Parse(
        string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var gcc =
            GccPattern.Match(line);

        if (gcc.Success)
            return CreateProblem(gcc);

        var dotnet =
            DotNetPattern.Match(line);

        if (dotnet.Success)
        {
            var problem =
                CreateProblem(dotnet);

            var code =
                dotnet.Groups["code"].Value;

            if (!string.IsNullOrWhiteSpace(code))
            {
                problem.Message =
                    $"{code}: {problem.Message}";
            }

            return problem;
        }

        var rust =
            RustInlinePattern.Match(line);

        if (rust.Success)
            return CreateProblem(rust);

        var python =
            PythonLocationPattern.Match(line);

        if (python.Success)
        {
            return new BuildProblem
            {
                FilePath =
                    python.Groups["file"].Value,

                Line =
                    ParseNumber(
                        python.Groups["line"].Value),

                Column = 0,

                Severity =
                    ProblemSeverity.Error,

                Message =
                    "Python traceback"
            };
        }

        return null;
    }

    private static BuildProblem CreateProblem(
        Match match)
    {
        return new BuildProblem
        {
            FilePath =
                match.Groups["file"]
                    .Value
                    .Trim(),

            Line =
                ParseNumber(
                    match.Groups["line"].Value),

            Column =
                ParseNumber(
                    match.Groups["column"].Value),

            Severity =
                ParseSeverity(
                    match.Groups["severity"].Value),

            Message =
                match.Groups["message"]
                    .Value
                    .Trim()
        };
    }

    private static int ParseNumber(
        string value)
    {
        return int.TryParse(
            value,
            out var number)
            ? number
            : 0;
    }

    private static ProblemSeverity ParseSeverity(
        string value)
    {
        var severity =
            value.Trim().ToLowerInvariant();

        if (severity.Contains("error"))
            return ProblemSeverity.Error;

        if (severity.Contains("warning"))
            return ProblemSeverity.Warning;

        return ProblemSeverity.Info;
    }
}