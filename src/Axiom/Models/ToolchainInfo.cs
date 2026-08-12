namespace Axiom.Models;

public sealed class ToolchainInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Command { get; init; }
    public required string[] ProbeCommands { get; init; }

    public Dictionary<string, string> LinuxPackages { get; init; } = new();

    public string? WingetId { get; init; }

    public string ShortName { get; init; } = "DEV";
}
