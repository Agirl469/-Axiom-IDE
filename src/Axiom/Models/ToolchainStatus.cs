namespace Axiom.Models;

public sealed class ToolchainStatus
{
    public required ToolchainInfo Toolchain { get; init; }
    public bool Installed { get; init; }
    public string Version { get; init; } = "Not found";
    public string InstallCommand { get; init; } = "No automatic install command available";
}
