using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Axiom.Effects;

public sealed class EffectTextureService
{
    private readonly Dictionary<string, Bitmap> _cache =
        new(StringComparer.Ordinal);

    public Bitmap? LoadBuiltIn(string relativePath)
    {
        var key = $"builtin:{relativePath}";

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var uri =
                new Uri(
                    $"avares://Axiom/Assets/Effects/{relativePath}");

            using var stream =
                AssetLoader.Open(uri);

            var bitmap =
                new Bitmap(stream);

            _cache[key] =
                bitmap;

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public Bitmap? LoadImported(
        string effectDirectory,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        try
        {
            var root =
                Path.GetFullPath(effectDirectory);

            var fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        root,
                        relativePath));

            var safeRoot =
                root.TrimEnd(
                    Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var comparison =
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!fullPath.StartsWith(
                    safeRoot,
                    comparison))
            {
                return null;
            }

            if (!File.Exists(fullPath))
                return null;

            var key =
                $"import:{fullPath}";

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            using var stream =
                File.OpenRead(fullPath);

            var bitmap =
                new Bitmap(stream);

            _cache[key] =
                bitmap;

            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}