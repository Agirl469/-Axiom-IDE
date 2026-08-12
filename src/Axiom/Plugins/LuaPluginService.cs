using System.Text.Json;

using MoonSharp.Interpreter;

namespace Axiom.Plugins;

public sealed class LuaPluginService
{
    public static LuaPluginService Current { get; } =
        new();

    private readonly List<LuaPlugin> _plugins =
        [];

    private readonly Dictionary<string, LuaCommand> _commands =
        new(StringComparer.OrdinalIgnoreCase);

    public string PluginDirectory { get; }

    public event Action<string>? OutputRequested;

    private LuaPluginService()
    {
        PluginDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Axiom",
                "Plugins");

        Directory.CreateDirectory(
            PluginDirectory);
    }

    public IReadOnlyCollection<string> Commands =>
        _commands.Keys;

    public async Task LoadAllAsync()
    {
        _plugins.Clear();
        _commands.Clear();

        foreach (var directory in
                 Directory.EnumerateDirectories(
                     PluginDirectory))
        {
            try
            {
                await LoadPluginAsync(
                    directory);
            }
            catch (Exception ex)
            {
                OutputRequested?.Invoke(
                    $"Plugin load failed: {ex.Message}");
            }
        }
    }

    public async Task LoadPluginAsync(
        string directory)
    {
        var manifestPath =
            Path.Combine(
                directory,
                "manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException(
                "manifest.json is missing.");
        }

        var json =
            await File.ReadAllTextAsync(
                manifestPath);

        var manifest =
            JsonSerializer
                .Deserialize<LuaPluginManifest>(
                    json)
            ?? throw new InvalidDataException(
                "Plugin manifest is invalid.");

        ValidateManifest(
            manifest);

        var entry =
            GetSafePluginPath(
                directory,
                manifest.Entry);

        if (!File.Exists(entry))
        {
            throw new FileNotFoundException(
                "Plugin entry file was not found.",
                entry);
        }

        var script =
            new Script(
                CoreModules.Preset_SoftSandbox);

        var plugin =
            new LuaPlugin
            {
                Directory =
                    directory,

                Manifest =
                    manifest,

                Script =
                    script
            };

        CreateAxiomApi(
            plugin);

        var source =
            await File.ReadAllTextAsync(
                entry);

        script.DoString(
            source);

        _plugins.Add(
            plugin);

        OutputRequested?.Invoke(
            $"Loaded plugin: {manifest.Name}");
    }

    public void RunCommand(
        string name)
    {
        if (!_commands.TryGetValue(
                name,
                out var command))
        {
            OutputRequested?.Invoke(
                $"Unknown Lua command: {name}");

            return;
        }

        try
        {
            command.Plugin.Script.Call(
                command.Function);
        }
        catch (ScriptRuntimeException ex)
        {
            OutputRequested?.Invoke(
                $"Lua error: {ex.DecoratedMessage}");
        }
    }

    private void CreateAxiomApi(
        LuaPlugin plugin)
    {
        var script =
            plugin.Script;

        var api =
            new Table(script);

        api["output"] =
            DynValue.NewCallback(
                (_, args) =>
                {
                    RequirePermission(
                        plugin,
                        "output.write");

                    var text =
                        args.Count > 0
                            ? args[0]
                                .CastToString()
                            : string.Empty;

                    OutputRequested?.Invoke(
                        text);

                    return DynValue.Nil;
                });

        api["register_command"] =
            DynValue.NewCallback(
                (_, args) =>
                {
                    RequirePermission(
                        plugin,
                        "commands.register");

                    if (args.Count < 2)
                    {
                        throw new ScriptRuntimeException(
                            "register_command requires a name and function.");
                    }

                    var name =
                        args[0]
                            .CastToString();

                    var function =
                        args[1];

                    if (function.Type !=
                        DataType.Function)
                    {
                        throw new ScriptRuntimeException(
                            "Second argument must be a Lua function.");
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        throw new ScriptRuntimeException(
                            "Command name cannot be empty.");
                    }

                    _commands[name] =
                        new LuaCommand(
                            plugin,
                            function);

                    return DynValue.Nil;
                });

        api["plugin_name"] =
            plugin.Manifest.Name;

        api["plugin_version"] =
            plugin.Manifest.Version;

        script.Globals["axiom"] =
            api;
    }

    private static void RequirePermission(
        LuaPlugin plugin,
        string permission)
    {
        if (plugin.Manifest.Permissions.Contains(
                permission,
                StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        throw new ScriptRuntimeException(
            $"Plugin does not have permission '{permission}'.");
    }

    private static void ValidateManifest(
        LuaPluginManifest manifest)
    {
        if (manifest.Format != 1)
        {
            throw new InvalidDataException(
                "Unsupported plugin format.");
        }

        if (string.IsNullOrWhiteSpace(
                manifest.Name))
        {
            throw new InvalidDataException(
                "Plugin name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                manifest.Entry))
        {
            throw new InvalidDataException(
                "Plugin entry is required.");
        }

        if (!manifest.Entry.EndsWith(
                ".lua",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Lua plugin entry must be a .lua file.");
        }
    }

    private static string GetSafePluginPath(
        string root,
        string relativePath)
    {
        var rootFull =
            Path.GetFullPath(root)
                .TrimEnd(
                    Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var candidate =
            Path.GetFullPath(
                Path.Combine(
                    root,
                    relativePath));

        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!candidate.StartsWith(
                rootFull,
                comparison))
        {
            throw new InvalidDataException(
                "Plugin attempted to access a path outside its folder.");
        }

        return candidate;
    }

    private sealed record LuaCommand(
        LuaPlugin Plugin,
        DynValue Function);
}