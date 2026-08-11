using Axiom.Packages;

namespace Axiom.Themes;

public sealed class ThemePackageService
{
    private readonly PackageReader _reader =
        new();

    private readonly PackageWriter _writer =
        new();

    public string ThemesDirectory { get; }

    public ThemePackageService()
    {
        ThemesDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Axiom",
                "Themes");

        Directory.CreateDirectory(
            ThemesDirectory);
    }

    public async Task<ImportedPackage> ImportAsync(
        string path)
    {
        var imported =
            await _reader.ImportAsync(
                path,
                ThemesDirectory);

        if (imported.Manifest.Type !=
            "theme")
        {
            try
            {
                Directory.Delete(
                    imported.Directory,
                    true);
            }
            catch
            {
            }

            throw new InvalidDataException(
                "This package is not an Axiom theme.");
        }

        return imported;
    }

    public async Task ExportAsync(
        string themeDirectory,
        string destination,
        AxiomTheme theme)
    {
        var manifest =
            new PackageManifest
            {
                Type = "theme",
                Name = theme.Name,
                Author = theme.Author,
                Entry = "theme.json"
            };

        await _writer.WriteAsync(
            themeDirectory,
            destination,
            manifest);
    }
}