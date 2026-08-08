namespace Axiom.Toolchains;

public sealed class ExternalToolchain
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Name { get; set; } =
        "Custom Toolchain";

    public string Language { get; set; } =
        "custom";

    public string ExecutablePath { get; set; } =
        string.Empty;

    public string BuildArguments { get; set; } =
        string.Empty;

    public string RunExecutable { get; set; } =
        string.Empty;

    public string RunArguments { get; set; } =
        string.Empty;

    public List<string> Extensions { get; set; } = [];

    public Dictionary<string, string> Environment { get; set; } =
        new();
}