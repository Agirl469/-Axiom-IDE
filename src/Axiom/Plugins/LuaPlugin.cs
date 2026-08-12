using MoonSharp.Interpreter;

namespace Axiom.Plugins;

internal sealed class LuaPlugin
{
    public required string Directory { get; init; }

    public required LuaPluginManifest Manifest { get; init; }

    public required Script Script { get; init; }
}