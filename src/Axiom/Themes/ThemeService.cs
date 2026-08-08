using Avalonia;
using Avalonia.Media;

namespace Axiom.Themes;

public sealed class ThemeService
{
    public AxiomTheme Current { get; private set; }

    public ThemeService()
    {
        Current = CreateAxiomDark();
    }

    public void Apply(AxiomTheme theme)
    {
        Current = theme;

        if (Application.Current is null)
            return;

        var resources =
            Application.Current.Resources;

        resources["AxiomBackground"] =
            Brush.Parse(theme.Ui.Background);

        resources["AxiomPanel"] =
            Brush.Parse(theme.Ui.Panel);

        resources["AxiomBorder"] =
            Brush.Parse(theme.Ui.Border);

        resources["AxiomForeground"] =
            Brush.Parse(theme.Ui.Foreground);

        resources["AxiomMuted"] =
            Brush.Parse(theme.Ui.Muted);

        resources["AxiomAccent"] =
            Brush.Parse(theme.Ui.Accent);

        resources["EditorBackground"] =
            Brush.Parse(theme.Editor.Background);

        resources["EditorForeground"] =
            Brush.Parse(theme.Editor.Foreground);

        resources["EditorSelection"] =
            Brush.Parse(theme.Editor.Selection);
    }

    public static AxiomTheme CreateAxiomDark()
    {
        return new AxiomTheme
        {
            Name = "Axiom Dark",
            Author = "Axiom",

            Ui = new ThemeUi
            {
                Background = "#18181B",
                Panel = "#202024",
                Border = "#303036",
                Foreground = "#E8E8EA",
                Muted = "#92929A",
                Accent = "#8B5CF6"
            },

            Editor = new ThemeEditor
            {
                Background = "#121214",
                Foreground = "#E8E8EA",
                Selection = "#35313D",
                CurrentLine = "#18181C"
            }
        };
    }

    public static AxiomTheme CreateLight()
    {
        return new AxiomTheme
        {
            Name = "Axiom Light",
            Author = "Axiom",

            Ui = new ThemeUi
            {
                Background = "#F5F6F8",
                Panel = "#FFFFFF",
                Border = "#D9DCE3",
                Foreground = "#202124",
                Muted = "#6B7078",
                Accent = "#6750A4"
            },

            Editor = new ThemeEditor
            {
                Background = "#FCFCFD",
                Foreground = "#202124",
                Selection = "#DAD2F2",
                CurrentLine = "#F1F2F5"
            },

            Syntax = new ThemeSyntax
            {
                Keyword = "#7B3FA1",
                String = "#317A3D",
                Number = "#B85C24",
                Comment = "#737980",
                Type = "#2E62A3",
                Function = "#8A5A00"
            }
        };
    }
}