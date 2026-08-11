namespace Axiom.Build;

public enum ProblemSeverity
{
    Error,
    Warning,
    Info
}

public sealed class BuildProblem
{
    public string FilePath { get; set; } =
        string.Empty;

    public int Line { get; set; }

    public int Column { get; set; }

    public string Message { get; set; } =
        string.Empty;

    public ProblemSeverity Severity { get; set; } =
        ProblemSeverity.Error;

    public override string ToString()
    {
        var location =
            Line > 0
                ? $"{FilePath}:{Line}:{Column}"
                : FilePath;

        return $"{Severity}: {location} - {Message}";
    }
}