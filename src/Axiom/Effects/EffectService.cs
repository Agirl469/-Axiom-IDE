using System.Text.Json;

namespace Axiom.Effects;

public sealed class EffectService
{
    public static EffectService Current { get; } =
        new();

    private readonly string _settingsPath;

    public EffectSettings Settings { get; private set; }

    public event EventHandler? SettingsChanged;

    public event EventHandler? PreviewRequested;

    private EffectService()
    {
        var folder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Axiom");

        Directory.CreateDirectory(folder);

        _settingsPath =
            Path.Combine(
                folder,
                "effects.json");

        Settings =
            Load();
    }

    private EffectSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new EffectSettings();

        try
        {
            var json =
                File.ReadAllText(
                    _settingsPath);

            return JsonSerializer.Deserialize<EffectSettings>(
                       json)
                   ?? new EffectSettings();
        }
        catch
        {
            return new EffectSettings();
        }
    }

    public async Task SaveAsync()
    {
        var json =
            JsonSerializer.Serialize(
                Settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            _settingsPath,
            json);

        SettingsChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void Update(
        Action<EffectSettings> update)
    {
        update(Settings);

        SettingsChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void Preview()
    {
        PreviewRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    public async Task ResetAsync()
    {
        Settings =
            new EffectSettings();

        await SaveAsync();
    }
}