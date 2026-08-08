using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Axiom.Themes;

namespace Axiom.Views;

public partial class ThemeEditorView : UserControl
{
    private readonly ThemeService _themes;

    private readonly TextBox _themeNameBox;
    private readonly TextBox _authorBox;

    private readonly TextBox _backgroundBox;
    private readonly TextBox _panelBox;
    private readonly TextBox _borderBox;
    private readonly TextBox _foregroundBox;
    private readonly TextBox _mutedBox;
    private readonly TextBox _accentBox;

    private readonly TextBox _editorBackgroundBox;
    private readonly TextBox _editorForegroundBox;
    private readonly TextBox _selectionBox;

    private readonly TextBlock _statusText;

    private readonly Border _previewBorder;
    private readonly Border _previewPanel;
    private readonly TextBlock _previewTitle;
    private readonly TextBlock _previewMuted;

    public ThemeEditorView(ThemeService themes)
    {
        _themes = themes;

        AvaloniaXamlLoader.Load(this);

        _themeNameBox =
            this.FindControl<TextBox>("ThemeNameBox")
            ?? throw new InvalidOperationException("ThemeNameBox was not found.");

        _authorBox =
            this.FindControl<TextBox>("AuthorBox")
            ?? throw new InvalidOperationException("AuthorBox was not found.");

        _backgroundBox =
            this.FindControl<TextBox>("BackgroundBox")
            ?? throw new InvalidOperationException("BackgroundBox was not found.");

        _panelBox =
            this.FindControl<TextBox>("PanelBox")
            ?? throw new InvalidOperationException("PanelBox was not found.");

        _borderBox =
            this.FindControl<TextBox>("BorderBox")
            ?? throw new InvalidOperationException("BorderBox was not found.");

        _foregroundBox =
            this.FindControl<TextBox>("ForegroundBox")
            ?? throw new InvalidOperationException("ForegroundBox was not found.");

        _mutedBox =
            this.FindControl<TextBox>("MutedBox")
            ?? throw new InvalidOperationException("MutedBox was not found.");

        _accentBox =
            this.FindControl<TextBox>("AccentBox")
            ?? throw new InvalidOperationException("AccentBox was not found.");

        _editorBackgroundBox =
            this.FindControl<TextBox>("EditorBackgroundBox")
            ?? throw new InvalidOperationException("EditorBackgroundBox was not found.");

        _editorForegroundBox =
            this.FindControl<TextBox>("EditorForegroundBox")
            ?? throw new InvalidOperationException("EditorForegroundBox was not found.");

        _selectionBox =
            this.FindControl<TextBox>("SelectionBox")
            ?? throw new InvalidOperationException("SelectionBox was not found.");

        _statusText =
            this.FindControl<TextBlock>("StatusText")
            ?? throw new InvalidOperationException("StatusText was not found.");

        _previewBorder =
            this.FindControl<Border>("PreviewBorder")
            ?? throw new InvalidOperationException("PreviewBorder was not found.");

        _previewPanel =
            this.FindControl<Border>("PreviewPanel")
            ?? throw new InvalidOperationException("PreviewPanel was not found.");

        _previewTitle =
            this.FindControl<TextBlock>("PreviewTitle")
            ?? throw new InvalidOperationException("PreviewTitle was not found.");

        _previewMuted =
            this.FindControl<TextBlock>("PreviewMuted")
            ?? throw new InvalidOperationException("PreviewMuted was not found.");

        LoadTheme(_themes.Current);
    }

    private void LoadTheme(AxiomTheme theme)
    {
        _themeNameBox.Text = theme.Name;
        _authorBox.Text = theme.Author;

        _backgroundBox.Text = theme.Ui.Background;
        _panelBox.Text = theme.Ui.Panel;
        _borderBox.Text = theme.Ui.Border;
        _foregroundBox.Text = theme.Ui.Foreground;
        _mutedBox.Text = theme.Ui.Muted;
        _accentBox.Text = theme.Ui.Accent;

        _editorBackgroundBox.Text = theme.Editor.Background;
        _editorForegroundBox.Text = theme.Editor.Foreground;
        _selectionBox.Text = theme.Editor.Selection;

        UpdatePreview(theme);
    }

    private AxiomTheme ReadTheme()
    {
        return new AxiomTheme
        {
            Name =
                string.IsNullOrWhiteSpace(_themeNameBox.Text)
                    ? "Custom Theme"
                    : _themeNameBox.Text.Trim(),

            Author =
                string.IsNullOrWhiteSpace(_authorBox.Text)
                    ? "Unknown"
                    : _authorBox.Text.Trim(),

            Ui = new ThemeUi
            {
                Background = NormalizeColor(_backgroundBox.Text),
                Panel = NormalizeColor(_panelBox.Text),
                Border = NormalizeColor(_borderBox.Text),
                Foreground = NormalizeColor(_foregroundBox.Text),
                Muted = NormalizeColor(_mutedBox.Text),
                Accent = NormalizeColor(_accentBox.Text)
            },

            Editor = new ThemeEditor
            {
                Background = NormalizeColor(_editorBackgroundBox.Text),
                Foreground = NormalizeColor(_editorForegroundBox.Text),
                Selection = NormalizeColor(_selectionBox.Text),
                CurrentLine = "#18181C"
            }
        };
    }

    private static string NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("A color field is empty.");

        var text = value.Trim();

        if (!text.StartsWith('#'))
            text = "#" + text;

        _ = Color.Parse(text);

        return text;
    }

    private void Apply_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var theme = ReadTheme();

            _themes.Apply(theme);

            UpdatePreview(theme);

            _statusText.Text =
                $"Applied '{theme.Name}'.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Theme error: {ex.Message}";
        }
    }

    private void ResetDark_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var theme =
            ThemeService.CreateAxiomDark();

        _themes.Apply(theme);
        LoadTheme(theme);

        _statusText.Text =
            "Restored Axiom Dark.";
    }

    private void ResetLight_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var theme =
            ThemeService.CreateLight();

        _themes.Apply(theme);
        LoadTheme(theme);

        _statusText.Text =
            "Restored Axiom Light.";
    }

    private void UpdatePreview(AxiomTheme theme)
    {
        _previewBorder.Background =
            Brush.Parse(theme.Ui.Background);

        _previewTitle.Foreground =
            Brush.Parse(theme.Ui.Foreground);

        _previewMuted.Foreground =
            Brush.Parse(theme.Ui.Muted);

        _previewPanel.Background =
            Brush.Parse(theme.Ui.Panel);

        _previewPanel.BorderBrush =
            Brush.Parse(theme.Ui.Border);

        _previewPanel.BorderThickness =
            new Avalonia.Thickness(1);
    }
}