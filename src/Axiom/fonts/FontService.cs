using Avalonia.Media;

namespace Axiom.Fonts;

public sealed class FontService
{
    public string FontDirectory { get; }

    public FontService()
    {
        FontDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Axiom",
            "Fonts");

        Directory.CreateDirectory(FontDirectory);
    }

    public IReadOnlyList<CustomFont> GetImportedFonts()
    {
        if (!Directory.Exists(FontDirectory))
            return [];

        return Directory
            .EnumerateFiles(FontDirectory)
            .Where(IsSupportedFont)
            .Select(path => new CustomFont
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FileName = Path.GetFileName(path),
                FilePath = path
            })
            .OrderBy(font => font.Name)
            .ToList();
    }

    public async Task<CustomFont> ImportAsync(
        string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(
                "The font file could not be found.",
                sourcePath);

        if (!IsSupportedFont(sourcePath))
        {
            throw new InvalidOperationException(
                "Axiom currently supports .ttf and .otf fonts.");
        }

        var fileName =
            Path.GetFileName(sourcePath);

        var destination =
            GetAvailableDestination(fileName);

        await using var source =
            File.OpenRead(sourcePath);

        await using var output =
            File.Create(destination);

        await source.CopyToAsync(output);

        return new CustomFont
        {
            Name =
                Path.GetFileNameWithoutExtension(destination),

            FileName =
                Path.GetFileName(destination),

            FilePath = destination
        };
    }

    public void Remove(CustomFont font)
    {
        if (File.Exists(font.FilePath))
            File.Delete(font.FilePath);
    }

    public IEnumerable<string> GetSystemFontNames()
    {
        return FontManager
            .Current
            .SystemFonts
            .Select(font => font.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name);
    }

    private string GetAvailableDestination(
        string fileName)
    {
        var destination =
            Path.Combine(
                FontDirectory,
                fileName);

        if (!File.Exists(destination))
            return destination;

        var baseName =
            Path.GetFileNameWithoutExtension(fileName);

        var extension =
            Path.GetExtension(fileName);

        var number = 2;

        while (true)
        {
            destination =
                Path.Combine(
                    FontDirectory,
                    $"{baseName}-{number}{extension}");

            if (!File.Exists(destination))
                return destination;

            number++;
        }
    }

    private static bool IsSupportedFont(
        string path)
    {
        var extension =
            Path.GetExtension(path);

        return extension.Equals(
                   ".ttf",
                   StringComparison.OrdinalIgnoreCase)
               ||
               extension.Equals(
                   ".otf",
                   StringComparison.OrdinalIgnoreCase);
    }
}