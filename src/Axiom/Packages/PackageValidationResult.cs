namespace Axiom.Packages;

public sealed class PackageValidationResult
{
    public bool IsValid =>
        Errors.Count == 0;

    public List<string> Errors { get; } =
        [];

    public List<string> Warnings { get; } =
        [];

    public PackageManifest? Manifest { get; set; }
}