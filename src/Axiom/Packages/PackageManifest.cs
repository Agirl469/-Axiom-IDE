using System.Text.Json.Serialization;

namespace Axiom.Packages;

public sealed class PackageManifest
{
    public int Format { get; set; } = 1;

    public string Type { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string Author { get; set; } =
        "Unknown";

    public string Version { get; set; } =
        "1.0.0";

    public string Entry { get; set; } =
        string.Empty;

    public List<PackageFile> Files { get; set; } =
        [];
}

public sealed class PackageFile
{
    public string Path { get; set; } =
        string.Empty;

    public string Sha256 { get; set; } =
        string.Empty;

    public long Size { get; set; }
}