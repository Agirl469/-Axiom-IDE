namespace Axiom.Themes;

public sealed class AxiomTheme
{
    public int Format { get; set; } = 1;

    public string Name { get; set; } = "Untitled";

    public string Author { get; set; } = "Unknown";

    public ThemeUi Ui { get; set; } = new();

    public ThemeEditor Editor { get; set; } = new();

    public ThemeSyntax Syntax { get; set; } = new();

    public ThemeTypography Typography { get; set; } = new();
}

public sealed class ThemeUi
{
    public string Background { get; set; } = "#18181B";
    public string Panel { get; set; } = "#202024";
    public string Border { get; set; } = "#303036";
    public string Foreground { get; set; } = "#E8E8EA";
    public string Muted { get; set; } = "#92929A";
    public string Accent { get; set; } = "#8B5CF6";
}

public sealed class ThemeEditor
{
    public string Background { get; set; } = "#121214";
    public string Foreground { get; set; } = "#E8E8EA";
    public string Selection { get; set; } = "#35313D";
    public string CurrentLine { get; set; } = "#18181C";
}

public sealed class ThemeSyntax
{
    public string Keyword { get; set; } = "#C792EA";
    public string String { get; set; } = "#C3E88D";
    public string Number { get; set; } = "#F78C6C";
    public string Comment { get; set; } = "#676E7B";
    public string Type { get; set; } = "#82AAFF";
    public string Function { get; set; } = "#FFCB6B";
}

public sealed class ThemeTypography
{
    public string UiFont { get; set; } =
        "Inter, Segoe UI, DejaVu Sans, sans-serif";

    public double UiFontSize { get; set; } = 14;

    public string EditorFont { get; set; } =
        "JetBrains Mono, Cascadia Mono, DejaVu Sans Mono, monospace";

    public double EditorFontSize { get; set; } = 14;
}