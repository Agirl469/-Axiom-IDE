using System.Text.Json.Serialization;

namespace Axiom.Models;

public sealed class AxiomProject
{
    [JsonPropertyName("format")]
    public int Format { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Untitled";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "plain";

    [JsonPropertyName("entry")]
    public string? Entry { get; set; }

    [JsonPropertyName("sourceRoots")]
    public List<string> SourceRoots { get; set; } = ["src"];

    [JsonPropertyName("settings")]
    public Dictionary<string, string> Settings { get; set; } = new();
}
