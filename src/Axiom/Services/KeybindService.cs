using System.Text.Json;
using Avalonia.Input;

namespace Axiom.Services;

public sealed class KeybindService
{
    public static KeybindService Current { get; } = new();

    private readonly string _filePath;

    public KeybindSettings Settings { get; private set; }

    private KeybindService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "Axiom");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(
            folder,
            "keybinds.json");

        Settings = Load();
    }

    private KeybindSettings Load()
    {
        if (!File.Exists(_filePath))
            return new KeybindSettings();

        try
        {
            var json =
                File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<KeybindSettings>(json)
                ?? new KeybindSettings();
        }
        catch
        {
            return new KeybindSettings();
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
            _filePath,
            json);
    }

    public async Task ResetAsync()
    {
        Settings =
            new KeybindSettings();

        await SaveAsync();
    }

    public bool Matches(
        KeyEventArgs e,
        Keybind bind)
    {
        return e.Key == bind.Key &&
               NormalizeModifiers(e.KeyModifiers) ==
               NormalizeModifiers(bind.Modifiers);
    }

    public bool HasConflict(
        string action,
        Keybind bind)
    {
        return GetBindings()
            .Any(x =>
                x.Action != action &&
                x.Bind.Key == bind.Key &&
                NormalizeModifiers(x.Bind.Modifiers) ==
                NormalizeModifiers(bind.Modifiers));
    }

    public string? FindConflict(
        string action,
        Keybind bind)
    {
        return GetBindings()
            .FirstOrDefault(x =>
                x.Action != action &&
                x.Bind.Key == bind.Key &&
                NormalizeModifiers(x.Bind.Modifiers) ==
                NormalizeModifiers(bind.Modifiers))
            ?.Action;
    }

    public IReadOnlyList<KeybindEntry> GetBindings()
    {
        return
        [
            new("Find", Settings.Find),
            new("Find in Project", Settings.FindProject),
            new("Save", Settings.Save),
            new("Save All", Settings.SaveAll),
            new("New File", Settings.NewFile),
            new("Build", Settings.Build),
            new("Run", Settings.Run),
            new("Stop", Settings.Stop),
            new("Terminal", Settings.Terminal),
            new("Close Tab", Settings.CloseTab)
        ];
    }

    public Keybind GetBinding(
        string action)
    {
        return action switch
        {
            "Save" => Settings.Save,
            "Save All" => Settings.SaveAll,
            "New File" => Settings.NewFile,
            "Build" => Settings.Build,
            "Run" => Settings.Run,
            "Stop" => Settings.Stop,
            "Terminal" => Settings.Terminal,
            "Close Tab" => Settings.CloseTab,
            "Find" => Settings.Find,
            "Find in Project" => Settings.FindProject,
            _ => throw new ArgumentException(
                $"Unknown keybind: {action}")
        };
    }

    public void SetBinding(
        string action,
        Keybind bind)
    {
        switch (action)
        {

            case "Find":
                Settings.Find = bind;
                break;

            case "Find in Project":
                Settings.FindProject = bind;
                break;


            case "Save":
                Settings.Save = bind;
                break;

            case "Save All":
                Settings.SaveAll = bind;
                break;

            case "New File":
                Settings.NewFile = bind;
                break;

            case "Build":
                Settings.Build = bind;
                break;

            case "Run":
                Settings.Run = bind;
                break;

            case "Stop":
                Settings.Stop = bind;
                break;

            case "Terminal":
                Settings.Terminal = bind;
                break;

            case "Close Tab":
                Settings.CloseTab = bind;
                break;
        }
    }

    public static string Format(
        Keybind bind)
    {
        var parts =
            new List<string>();

        if (bind.Modifiers.HasFlag(
                KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (bind.Modifiers.HasFlag(
                KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (bind.Modifiers.HasFlag(
                KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (bind.Modifiers.HasFlag(
                KeyModifiers.Meta))
        {
            parts.Add("Meta");
        }

        parts.Add(
            GetKeyName(bind.Key));

        return string.Join(
            " + ",
            parts);
    }

    public static bool IsModifierKey(
        Key key)
    {
        return key is
            Key.LeftCtrl or
            Key.RightCtrl or
            Key.LeftShift or
            Key.RightShift or
            Key.LeftAlt or
            Key.RightAlt or
            Key.LWin or
            Key.RWin;
    }

    private static string GetKeyName(
        Key key)
    {
        return key switch
        {
            Key.OemTilde => "`",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            _ => key.ToString()
        };
    }

    private static KeyModifiers NormalizeModifiers(
        KeyModifiers modifiers)
    {
        return modifiers &
            (KeyModifiers.Control |
             KeyModifiers.Shift |
             KeyModifiers.Alt |
             KeyModifiers.Meta);
    }
}

public sealed class KeybindSettings
{

    public Keybind Find { get; set; } =
    new(
        Key.F,
        KeyModifiers.Control);

    public Keybind FindProject { get; set; } =
        new(
            Key.F,
            KeyModifiers.Control |
            KeyModifiers.Shift);

    public Keybind Save { get; set; } =
        new(Key.S, KeyModifiers.Control);

    public Keybind SaveAll { get; set; } =
        new(
            Key.S,
            KeyModifiers.Control |
            KeyModifiers.Shift);

    public Keybind NewFile { get; set; } =
        new(Key.N, KeyModifiers.Control);

    public Keybind Build { get; set; } =
        new(
            Key.B,
            KeyModifiers.Control |
            KeyModifiers.Shift);

    public Keybind Run { get; set; } =
        new(Key.F5);

    public Keybind Stop { get; set; } =
        new(
            Key.F5,
            KeyModifiers.Shift);

    public Keybind Terminal { get; set; } =
        new(
            Key.OemTilde,
            KeyModifiers.Control);

    public Keybind CloseTab { get; set; } =
        new(Key.W, KeyModifiers.Control);
}

public sealed class Keybind
{
    public Keybind()
    {
    }

    public Keybind(
        Key key,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public Key Key { get; set; }

    public KeyModifiers Modifiers { get; set; }
}

public sealed record KeybindEntry(
    string Action,
    Keybind Bind);