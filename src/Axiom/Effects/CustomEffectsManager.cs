using System.Text.Json;

namespace Axiom.Effects;

public sealed class CustomEffectsManager
{
    private readonly EffectPackageService _packages =
        new();

    public IReadOnlyList<InstalledEffect> GetInstalled()
    {
        var result =
            new List<InstalledEffect>();

        if (!Directory.Exists(
                _packages.EffectsDirectory))
        {
            return result;
        }

        foreach (var directory in
                 Directory.EnumerateDirectories(
                     _packages.EffectsDirectory))
        {
            var definition =
                Path.Combine(
                    directory,
                    "effect.json");

            if (!File.Exists(definition))
                continue;

            try
            {
                var json =
                    File.ReadAllText(
                        definition);

                var effect =
                    JsonSerializer.Deserialize
                        <CustomEffectDefinition>(
                            json);

                if (effect is null)
                    continue;

                result.Add(
                    new InstalledEffect
                    {
                        Name =
                            effect.Name,

                        Author =
                            effect.Author,

                        Directory =
                            directory,

                        DefinitionPath =
                            definition
                    });
            }
            catch
            {
            }
        }

        return result
            .OrderBy(
                effect =>
                    effect.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsEnabled(
        InstalledEffect effect)
    {
        return EffectService
            .Current
            .Settings
            .EnabledCustomEffects
            .Contains(
                effect.Directory,
                StringComparer.Ordinal);
    }

    public void SetEnabled(
        InstalledEffect effect,
        bool enabled)
    {
        var list =
            EffectService
                .Current
                .Settings
                .EnabledCustomEffects;

        list.RemoveAll(
            path =>
                path.Equals(
                    effect.Directory,
                    StringComparison.Ordinal));

        if (enabled)
            list.Add(effect.Directory);

        EffectService.Current.Update(
            _ =>
            {
            });
    }

    public void Remove(
        InstalledEffect effect)
    {
        SetEnabled(
            effect,
            false);

        if (Directory.Exists(
                effect.Directory))
        {
            Directory.Delete(
                effect.Directory,
                true);
        }
    }

    public void OpenFolder(
        InstalledEffect effect)
    {
        if (OperatingSystem.IsLinux())
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName =
                        "xdg-open",

                    ArgumentList =
                    {
                        effect.Directory
                    },

                    UseShellExecute =
                        false
                });

            return;
        }

        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName =
                        "explorer.exe",

                    ArgumentList =
                    {
                        effect.Directory
                    }
                });
        }
    }
}

public sealed class InstalledEffect
{
    public string Name { get; set; } =
        string.Empty;

    public string Author { get; set; } =
        "Unknown";

    public string Directory { get; set; } =
        string.Empty;

    public string DefinitionPath { get; set; } =
        string.Empty;

    public override string ToString()
    {
        return $"{Name} — {Author}";
    }
}