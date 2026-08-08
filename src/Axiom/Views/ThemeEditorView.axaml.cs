using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using Axiom.Fonts;
using Axiom.Themes;

namespace Axiom.Views;

public partial class ThemeEditorView : UserControl
{
    private readonly ThemeService _themes;
    private readonly FontService _fonts = new();

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

    private readonly ComboBox _uiFontBox;
    private readonly TextBox _uiFontSizeBox;

    private readonly ComboBox _editorFontBox;
    private readonly TextBox _editorFontSizeBox;

    private readonly ComboBox _syntaxPresetBox;

    private readonly TextBlock _statusText;
    private readonly TextBlock _fontStatusText;

    private readonly Border _previewBorder;
    private readonly Border _previewPanel;

    private readonly TextBlock _previewTitle;
    private readonly TextBlock _previewMuted;

    // AXAML events can fire while AvaloniaXamlLoader.Load(this)
    // is still constructing the view.
    private bool _initializing = true;

    public ThemeEditorView(ThemeService themes)
    {
        _themes = themes;

        AvaloniaXamlLoader.Load(this);

        _themeNameBox =
            FindRequired<TextBox>("ThemeNameBox");

        _authorBox =
            FindRequired<TextBox>("AuthorBox");

        _backgroundBox =
            FindRequired<TextBox>("BackgroundBox");

        _panelBox =
            FindRequired<TextBox>("PanelBox");

        _borderBox =
            FindRequired<TextBox>("BorderBox");

        _foregroundBox =
            FindRequired<TextBox>("ForegroundBox");

        _mutedBox =
            FindRequired<TextBox>("MutedBox");

        _accentBox =
            FindRequired<TextBox>("AccentBox");

        _editorBackgroundBox =
            FindRequired<TextBox>("EditorBackgroundBox");

        _editorForegroundBox =
            FindRequired<TextBox>("EditorForegroundBox");

        _selectionBox =
            FindRequired<TextBox>("SelectionBox");

        _uiFontBox =
            FindRequired<ComboBox>("UiFontBox");

        _uiFontSizeBox =
            FindRequired<TextBox>("UiFontSizeBox");

        _editorFontBox =
            FindRequired<ComboBox>("EditorFontBox");

        _editorFontSizeBox =
            FindRequired<TextBox>("EditorFontSizeBox");

        _syntaxPresetBox =
            FindRequired<ComboBox>("SyntaxPresetBox");

        _fontStatusText =
            FindRequired<TextBlock>("FontStatusText");

        _statusText =
            FindRequired<TextBlock>("StatusText");

        _previewBorder =
            FindRequired<Border>("PreviewBorder");

        _previewPanel =
            FindRequired<Border>("PreviewPanel");

        _previewTitle =
            FindRequired<TextBlock>("PreviewTitle");

        _previewMuted =
            FindRequired<TextBlock>("PreviewMuted");

        RefreshFontLists();
        LoadTheme(_themes.Current);

        _initializing = false;
    }

    private T FindRequired<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException(
                $"{name} was not found in ThemeEditorView.axaml.");
    }

    private void LoadTheme(AxiomTheme theme)
    {
        _themeNameBox.Text =
            theme.Name;

        _authorBox.Text =
            theme.Author;

        _backgroundBox.Text =
            theme.Ui.Background;

        _panelBox.Text =
            theme.Ui.Panel;

        _borderBox.Text =
            theme.Ui.Border;

        _foregroundBox.Text =
            theme.Ui.Foreground;

        _mutedBox.Text =
            theme.Ui.Muted;

        _accentBox.Text =
            theme.Ui.Accent;

        _editorBackgroundBox.Text =
            theme.Editor.Background;

        _editorForegroundBox.Text =
            theme.Editor.Foreground;

        _selectionBox.Text =
            theme.Editor.Selection;

        _uiFontSizeBox.Text =
            theme.Typography.UiFontSize.ToString();

        _editorFontSizeBox.Text =
            theme.Typography.EditorFontSize.ToString();

        SelectFont(
            _uiFontBox,
            theme.Typography.UiFont);

        SelectFont(
            _editorFontBox,
            theme.Typography.EditorFont);

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
                Background =
                    NormalizeColor(
                        _backgroundBox.Text),

                Panel =
                    NormalizeColor(
                        _panelBox.Text),

                Border =
                    NormalizeColor(
                        _borderBox.Text),

                Foreground =
                    NormalizeColor(
                        _foregroundBox.Text),

                Muted =
                    NormalizeColor(
                        _mutedBox.Text),

                Accent =
                    NormalizeColor(
                        _accentBox.Text)
            },

            Editor = new ThemeEditor
            {
                Background =
                    NormalizeColor(
                        _editorBackgroundBox.Text),

                Foreground =
                    NormalizeColor(
                        _editorForegroundBox.Text),

                Selection =
                    NormalizeColor(
                        _selectionBox.Text),

                CurrentLine =
                    _themes.Current.Editor.CurrentLine
            },

            Syntax =
                GetSelectedSyntaxPreset(),

            Typography = new ThemeTypography
            {
                UiFont =
                    GetSelectedFont(
                        _uiFontBox,
                        "DejaVu Sans"),

                UiFontSize =
                    ReadFontSize(
                        _uiFontSizeBox.Text,
                        14),

                EditorFont =
                    GetSelectedFont(
                        _editorFontBox,
                        "DejaVu Sans Mono"),

                EditorFontSize =
                    ReadFontSize(
                        _editorFontSizeBox.Text,
                        14)
            }
        };
    }

    private static string NormalizeColor(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "A color field is empty.");
        }

        var text =
            value.Trim();

        if (!text.StartsWith('#'))
            text = "#" + text;

        _ = Color.Parse(text);

        return text;
    }

    private static double ReadFontSize(
        string? value,
        double fallback)
    {
        if (!double.TryParse(
                value,
                out var size))
        {
            return fallback;
        }

        return Math.Clamp(
            size,
            8,
            32);
    }

    private static string GetSelectedFont(
        ComboBox comboBox,
        string fallback)
    {
        if (comboBox.SelectedItem is string font &&
            !string.IsNullOrWhiteSpace(font))
        {
            return font;
        }

        return fallback;
    }

    private static void SelectFont(
        ComboBox comboBox,
        string font)
    {
        if (comboBox.ItemsSource
            is not IEnumerable<string> fonts)
        {
            return;
        }

        // Theme defaults may contain fallback lists:
        // "JetBrains Mono, Cascadia Mono, ..."
        // Try each family until one installed font matches.
        var requestedFonts =
            font.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        foreach (var requested in requestedFonts)
        {
            var match =
                fonts.FirstOrDefault(
                    item =>
                        string.Equals(
                            item,
                            requested,
                            StringComparison.OrdinalIgnoreCase));

            if (match is null)
                continue;

            comboBox.SelectedItem = match;
            return;
        }
    }

    private void RefreshFontLists()
    {
        var currentUi =
            GetSelectedFont(
                _uiFontBox,
                string.Empty);

        var currentEditor =
            GetSelectedFont(
                _editorFontBox,
                string.Empty);

        var fonts =
            _fonts
                .GetSystemFontNames()
                .Concat(
                    _fonts
                        .GetImportedFonts()
                        .Select(font => font.Name))
                .Where(
                    font =>
                        !string.IsNullOrWhiteSpace(font))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    name => name)
                .ToList();

        _uiFontBox.ItemsSource =
            fonts;

        _editorFontBox.ItemsSource =
            fonts;

        if (!string.IsNullOrWhiteSpace(currentUi))
        {
            SelectFont(
                _uiFontBox,
                currentUi);
        }

        if (!string.IsNullOrWhiteSpace(currentEditor))
        {
            SelectFont(
                _editorFontBox,
                currentEditor);
        }
    }

    private void Apply_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var theme =
                ReadTheme();

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
        _initializing = true;

        try
        {
            var theme =
                ThemeService.CreateAxiomDark();

            _themes.Apply(theme);

            RefreshFontLists();
            LoadTheme(theme);

            _statusText.Text =
                "Restored Axiom Dark.";
        }
        finally
        {
            _initializing = false;
        }
    }

    private void ResetLight_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _initializing = true;

        try
        {
            var theme =
                ThemeService.CreateLight();

            _themes.Apply(theme);

            RefreshFontLists();
            LoadTheme(theme);

            _statusText.Text =
                "Restored Axiom Light.";
        }
        finally
        {
            _initializing = false;
        }
    }

    private async void ImportFont_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return;

        var files =
            await topLevel.StorageProvider
                .OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Import Font",

                        AllowMultiple = false,

                        FileTypeFilter =
                        [
                            new FilePickerFileType(
                                "Font Files")
                            {
                                Patterns =
                                [
                                    "*.ttf",
                                    "*.otf"
                                ]
                            }
                        ]
                    });

        if (files.Count == 0)
            return;

        var source =
            files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(source))
            return;

        try
        {
            var font =
                await _fonts.ImportAsync(source);

            _fontStatusText.Text =
                $"Imported {font.FileName}";

            RefreshFontLists();
        }
        catch (Exception ex)
        {
            _fontStatusText.Text =
                $"Could not import font: {ex.Message}";
        }
    }

    private void RefreshFonts_Click(
        object? sender,
        RoutedEventArgs e)
    {
        RefreshFontLists();

        _fontStatusText.Text =
            "Font list refreshed.";
    }

    private void SyntaxPreset_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        // This event can fire while AXAML is loading.
        // Do not touch the editor fields until initialization is complete.
        if (_initializing)
            return;

        try
        {
            var theme =
                ReadTheme();

            UpdatePreview(theme);

            if (_syntaxPresetBox.SelectedItem
                is ComboBoxItem item)
            {
                _statusText.Text =
                    $"Code colors: {item.Content}";
            }
        }
        catch (Exception ex)
        {
            _statusText.Text =
                $"Preview error: {ex.Message}";
        }
    }

    private ThemeSyntax GetSelectedSyntaxPreset()
    {
        if (_syntaxPresetBox.SelectedItem
            is ComboBoxItem item)
        {
            var tag =
                item.Tag?
                    .ToString()?
                    .ToLowerInvariant();

            return tag switch
            {
                "midnight" =>
                    SyntaxPresets.Midnight(),

                "rose" =>
                    SyntaxPresets.Rose(),

                "forest" =>
                    SyntaxPresets.Forest(),

                "ocean" =>
                    SyntaxPresets.Ocean(),

                "paper" =>
                    SyntaxPresets.Paper(),

                _ =>
                    SyntaxPresets.Axiom()
            };
        }

        return _themes.Current.Syntax;
    }

    private void UpdatePreview(
        AxiomTheme theme)
    {
        _previewBorder.Background =
            Brush.Parse(
                theme.Ui.Background);

        _previewBorder.BorderBrush =
            Brush.Parse(
                theme.Ui.Border);

        _previewBorder.BorderThickness =
            new Thickness(1);

        _previewPanel.Background =
            Brush.Parse(
                theme.Ui.Panel);

        _previewPanel.BorderBrush =
            Brush.Parse(
                theme.Ui.Border);

        _previewPanel.BorderThickness =
            new Thickness(1);

        _previewTitle.Foreground =
            Brush.Parse(
                theme.Ui.Foreground);

        _previewMuted.Foreground =
            Brush.Parse(
                theme.Ui.Muted);

        var previewFont =
            new FontFamily(
                theme.Typography.UiFont);

        _previewTitle.FontFamily =
            previewFont;

        _previewTitle.FontSize =
            theme.Typography.UiFontSize;

        _previewMuted.FontFamily =
            previewFont;

        _previewMuted.FontSize =
            theme.Typography.UiFontSize;
    }
}