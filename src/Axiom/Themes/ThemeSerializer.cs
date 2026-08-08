using System.Text.Json;

namespace Axiom.Themes;

public static class ThemeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(AxiomTheme theme)
    {
        return JsonSerializer.Serialize(theme, Options);
    }

    public static AxiomTheme Deserialize(string json)
    {
        var theme = JsonSerializer.Deserialize<AxiomTheme>(json, Options);

        if (theme is null)
            throw new InvalidOperationException("Theme data is invalid.");

        if (theme.Format != 1)
            throw new InvalidOperationException(
                $"Unsupported theme format: {theme.Format}");

        return theme;
    }
}