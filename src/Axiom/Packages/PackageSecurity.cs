namespace Axiom.Packages;

public static class PackageSecurity
{
    public const int MaxFiles = 64;

    public const long MaxSingleFileSize =
        10 * 1024 * 1024;

    public const long MaxTotalSize =
        40 * 1024 * 1024;

    private static readonly HashSet<string> BlockedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".dll",
            ".com",
            ".scr",
            ".msi",

            ".bat",
            ".cmd",
            ".ps1",
            ".psm1",

            ".sh",
            ".bash",
            ".zsh",
            ".fish",

            ".vbs",
            ".vbe",
            ".js",
            ".jse",
            ".wsf",

            ".jar",
            ".class",

            ".so",
            ".dylib"
        };

    private static readonly HashSet<string> AllowedEffectExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".json",
            ".png",
            ".jpg",
            ".jpeg",
            ".webp"
        };

    private static readonly HashSet<string> AllowedThemeExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".json",
            ".png",
            ".jpg",
            ".jpeg",
            ".webp",
            ".ttf",
            ".otf"
        };

    public static bool IsBlockedFile(
        string path)
    {
        var extension =
            Path.GetExtension(path);

        return BlockedExtensions.Contains(
            extension);
    }

    public static bool IsAllowedFile(
        string packageType,
        string path)
    {
        if (IsBlockedFile(path))
            return false;

        var extension =
            Path.GetExtension(path);

        return packageType switch
        {
            "effect" =>
                AllowedEffectExtensions.Contains(
                    extension),

            "theme" =>
                AllowedThemeExtensions.Contains(
                    extension),

            _ =>
                false
        };
    }

    public static bool IsSafeArchivePath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized =
            path.Replace(
                '\\',
                '/');

        if (normalized.StartsWith('/'))
            return false;

        if (normalized.Contains(
                "../",
                StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.Equals(
                "..",
                StringComparison.Ordinal))
        {
            return false;
        }

        if (Path.IsPathRooted(path))
            return false;

        return true;
    }

    public static string GetSafeOutputPath(
        string root,
        string relativePath)
    {
        if (!IsSafeArchivePath(
                relativePath))
        {
            throw new InvalidDataException(
                "Package contains an unsafe path.");
        }

        var normalized =
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar);

        var rootFull =
            Path.GetFullPath(root)
                .TrimEnd(
                    Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var result =
            Path.GetFullPath(
                Path.Combine(
                    root,
                    normalized));

        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!result.StartsWith(
                rootFull,
                comparison))
        {
            throw new InvalidDataException(
                "Package attempted to write outside its destination.");
        }

        return result;
    }

    public static string MakeSafeFolderName(
        string name)
    {
        var invalid =
            Path.GetInvalidFileNameChars();

        var cleaned =
            new string(
                name
                    .Select(
                        c =>
                            invalid.Contains(c)
                                ? '_'
                                : c)
                    .ToArray());

        cleaned =
            cleaned.Trim();

        return string.IsNullOrWhiteSpace(cleaned)
            ? "Imported"
            : cleaned;
    }
}