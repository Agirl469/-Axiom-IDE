using System.Text.Json;

using Axiom.Packages;

namespace Axiom.Effects;

public sealed class EffectPackageService
{
    private readonly PackageReader _reader =
        new();

    private readonly PackageWriter _writer =
        new();

    public string EffectsDirectory { get; }

    public EffectPackageService()
    {
        EffectsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Axiom",
                "Effects");

        Directory.CreateDirectory(
            EffectsDirectory);
    }

    public async Task<ImportedPackage> ImportAsync(
        string packagePath)
    {
        var imported =
            await _reader.ImportAsync(
                packagePath,
                EffectsDirectory);

        if (imported.Manifest.Type !=
            "effect")
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
                "This package is not an Axiom effect.");
        }

        await ValidateEffectDefinitionAsync(
            imported.EntryPath);

        return imported;
    }

    public async Task ExportAsync(
        string effectDirectory,
        string destination)
    {
        var definitionPath =
            Path.Combine(
                effectDirectory,
                "effect.json");

        if (!File.Exists(
                definitionPath))
        {
            throw new FileNotFoundException(
                "effect.json was not found.");
        }

        var definition =
            await LoadDefinitionAsync(
                definitionPath);

        var manifest =
            new PackageManifest
            {
                Type = "effect",
                Name = definition.Name,
                Author = definition.Author,
                Entry = "effect.json"
            };

        await _writer.WriteAsync(
            effectDirectory,
            destination,
            manifest);
    }

    public async Task<CustomEffectDefinition> LoadDefinitionAsync(
        string path)
    {
        await using var stream =
            File.OpenRead(path);

        return await JsonSerializer
                   .DeserializeAsync<CustomEffectDefinition>(
                       stream)
               ?? throw new InvalidDataException(
                   "Effect definition is invalid.");
    }

    private async Task ValidateEffectDefinitionAsync(
        string path)
    {
        var definition =
            await LoadDefinitionAsync(
                path);

        if (definition.Format != 1)
        {
            throw new InvalidDataException(
                "Unsupported effect format.");
        }

        if (definition.Type !=
            "particle")
        {
            throw new InvalidDataException(
                $"Unsupported effect type: {definition.Type}");
        }

        definition.Spawn.Rate =
            Math.Clamp(
                definition.Spawn.Rate,
                0,
                50);

        definition.Particle.SizeMin =
            Math.Clamp(
                definition.Particle.SizeMin,
                2,
                128);

        definition.Particle.SizeMax =
            Math.Clamp(
                definition.Particle.SizeMax,
                definition.Particle.SizeMin,
                128);

        definition.Particle.Opacity =
            Math.Clamp(
                definition.Particle.Opacity,
                0,
                1);

        definition.Particle.Lifetime =
            Math.Clamp(
                definition.Particle.Lifetime,
                0.1,
                60);

        definition.Motion.SpeedMin =
            Math.Clamp(
                definition.Motion.SpeedMin,
                -500,
                500);

        definition.Motion.SpeedMax =
            Math.Clamp(
                definition.Motion.SpeedMax,
                -500,
                500);

        definition.Motion.Wind =
            Math.Clamp(
                definition.Motion.Wind,
                -500,
                500);

        definition.Motion.Drift =
            Math.Clamp(
                definition.Motion.Drift,
                0,
                500);
    }
}