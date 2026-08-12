namespace Axiom.Plugins;

public sealed class LuaPluginManifest
{
    public int Format { get; set; } = 1;

    public string Name { get; set; } =
        "Unnamed Plugin";

    public string Author { get; set; } =
        "Unknown";

    public string Version { get; set; } =
        "1.0.0";

    public string Entry { get; set; } =
        "plugin.lua";

    public List<string> Permissions { get; set; } =
        [];
}