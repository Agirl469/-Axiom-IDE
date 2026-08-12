namespace Axiom.Models;

public sealed class ToolchainBundle
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string[] ToolchainIds { get; init; }

    public string ShortName { get; init; } =
        "DEV";
}