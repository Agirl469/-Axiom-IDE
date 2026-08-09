using System.Text.Json;
using Avalonia.Input;

namespace Axiom.Services;

public sealed class KeybindService
{
    private readonly string _path;

    public KeybindSettings Settings { get; private set; } = new();

    public KeybindService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "Axiom");

        Directory.CreateDirectory(directory);

        _path = Path.Combine(directory, "keybinds.json");

        Load();
    }

    public void Load()
    {
        if (!File.Exists(_path))
        {
            Settings = new KeybindSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);

            Settings =
                JsonSerializer.Deserialize<KeybindSettings>(json)
                ?? new KeybindSettings();
        }
        catch
        {
            Settings = new KeybindSettings();
        }
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(
            Settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(_path, json);
    }

    public bool Matches(
        KeyEventArgs e,
        string binding)
    {
        if (string.IsNullOrWhiteSpace(binding))
            return false;

        var parts = binding
            .Split(
                '+',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var modifiers = KeyModifiers.None;
        Key? key = null;

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;

                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;

                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;

                case "cmd":
                case "command":
                case "meta":
                    modifiers |= KeyModifiers.Meta;
                    break;

                default:
                    if (Enum.TryParse<Key>(
                        part,
                        true,
                        out var parsed))
                    {
                        key = parsed;
                    }
                    break;
            }
        }

        return key is not null &&
               e.Key == key &&
               e.KeyModifiers == modifiers;
    }

    public void Reset()
    {
        Settings = new KeybindSettings();
    }
}

public sealed class KeybindSettings
{
    public string Save { get; set; } = "Ctrl+S";
    public string SaveAll { get; set; } = "Ctrl+Shift+S";

    public string NewFile { get; set; } = "Ctrl+N";
    public string OpenFile { get; set; } = "Ctrl+O";

    public string Find { get; set; } = "Ctrl+F";
    public string FindProject { get; set; } = "Ctrl+Shift+F";

    public string Build { get; set; } = "Ctrl+Shift+B";
    public string Run { get; set; } = "F5";
    public string Stop { get; set; } = "Shift+F5";

    public string Terminal { get; set; } = "Ctrl+OemTilde";
    public string CloseTab { get; set; } = "Ctrl+W";

    public string Settings { get; set; } = "Ctrl+OemComma";
}