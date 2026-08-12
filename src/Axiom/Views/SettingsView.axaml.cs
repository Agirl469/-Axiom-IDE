using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Axiom.Views;

public partial class SettingsView : UserControl
{
    private readonly ListBox _categoryList;

    private readonly ContentControl _settingsHost;

    private readonly TextBlock _pageTitle;

    private readonly TextBlock _pageDescription;

    public SettingsView()
    {
        AvaloniaXamlLoader.Load(this);

        _categoryList =
            Get<ListBox>(
                "CategoryList");

        _settingsHost =
            Get<ContentControl>(
                "SettingsHost");

        _pageTitle =
            Get<TextBlock>(
                "PageTitle");

        _pageDescription =
            Get<TextBlock>(
                "PageDescription");

        _categoryList.SelectedIndex =
            0;

        ShowGeneral();
    }

    private T Get<T>(
        string name)
        where T : Control
    {
        return this.FindControl<T>(
                   name)
               ?? throw new InvalidOperationException(
                   $"{name} was not found.");
    }

    private void CategoryList_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_categoryList.SelectedItem
            is not ListBoxItem item)
        {
            return;
        }

        var category =
            item.Tag?.ToString();

        switch (category)
        {
            case "appearance":
                ShowAppearance();
                break;

            case "editor":
                ShowEditor();
                break;

            case "keybinds":
                ShowKeybinds();
                break;

            case "effects":
                ShowEffects();
                break;

            case "plugins":
                ShowPlugins();
                break;

            case "toolchains":
                ShowToolchains();
                break;

            case "privacy":
                ShowPrivacy();
                break;

            default:
                ShowGeneral();
                break;
        }
    }

    private void SetPage(
        string title,
        string description,
        Control content)
    {
        _pageTitle.Text =
            title;

        _pageDescription.Text =
            description;

        _settingsHost.Content =
            content;
    }

    private void ShowGeneral()
    {
        var content =
            new StackPanel
            {
                Spacing = 14
            };

        content.Children.Add(
            CreateSection(
                "Startup",

                new CheckBox
                {
                    Content =
                        "Restore previous workspace",

                    IsChecked =
                        true
                },

                new CheckBox
                {
                    Content =
                        "Open last project on startup"
                },

                new CheckBox
                {
                    Content =
                        "Check toolchains on startup",

                    IsChecked =
                        true
                }));


        content.Children.Add(
            CreateSection(
                "Interface",

                new CheckBox
                {
                    Content =
                        "Show status bar",

                    IsChecked =
                        true
                },

                new CheckBox
                {
                    Content =
                        "Use compact controls",

                    IsChecked =
                        true
                }));


        SetPage(
            "General",
            "Startup and interface preferences.",
            content);
    }

    private void ShowAppearance()
    {
        var content =
            new StackPanel
            {
                Spacing = 14
            };

        content.Children.Add(
            CreateSection(
                "Themes",

                new TextBlock
                {
                    Text =
                        "Use the Themes menu to create, import and edit Axiom themes.",

                    TextWrapping =
                        TextWrapping.Wrap
                },

                new TextBlock
                {
                    Text =
                        "Theme colors, UI fonts and editor fonts remain fully customizable.",

                    TextWrapping =
                        TextWrapping.Wrap,

                    Classes =
                    {
                        "muted"
                    }
                }));

        SetPage(
            "Appearance",
            "Themes, colors and typography.",
            content);
    }

    private void ShowEditor()
    {
        var fontSize =
            new NumericUpDown
            {
                Minimum = 9,
                Maximum = 32,
                Value = 14,
                Width = 120
            };

        var tabSize =
            new NumericUpDown
            {
                Minimum = 1,
                Maximum = 8,
                Value = 4,
                Width = 120
            };

        var content =
            new StackPanel
            {
                Spacing = 14
            };

        content.Children.Add(
            CreateSection(
                "Text",

                CreateRow(
                    "Font size",
                    fontSize),

                CreateRow(
                    "Tab size",
                    tabSize)));


        content.Children.Add(
            CreateSection(
                "Editing",

                new CheckBox
                {
                    Content =
                        "Smart indentation",

                    IsChecked =
                        true
                },

                new CheckBox
                {
                    Content =
                        "Restore open files",

                    IsChecked =
                        true
                },

                new CheckBox
                {
                    Content =
                        "Restore cursor position",

                    IsChecked =
                        true
                }));


        SetPage(
            "Editor",
            "Code editing behavior.",
            content);
    }

    private void ShowKeybinds()
    {
        SetPage(
            "Keybinds",
            "Customize keyboard shortcuts.",
            new KeybindsView());
    }

    private void ShowEffects()
    {
        SetPage(
            "Effects",
            "Optional particles and visual effects. Disabled by default.",
            new EffectsSettingsView());
    }

    private void ShowPlugins()
    {
        var content =
            new StackPanel
            {
                Spacing = 14
            };

        content.Children.Add(
            CreateSection(
                "Lua Plugins",

                new TextBlock
                {
                    Text =
                        "Axiom Lua plugins extend the editor with commands and other features.",

                    TextWrapping =
                        TextWrapping.Wrap
                },

                new TextBlock
                {
                    Text =
                        "Plugins are separate from themes and effects because plugins contain executable Lua code.",

                    TextWrapping =
                        TextWrapping.Wrap,

                    Classes =
                    {
                        "muted"
                    }
                }));

        SetPage(
            "Plugins",
            "Manage Lua extensions.",
            content);
    }

    private void ShowToolchains()
    {
        SetPage(
            "Toolchains",
            "Compilers, SDKs and development tools.",
            new ToolchainsView());
    }

    private void ShowPrivacy()
    {
        var content =
            new StackPanel
            {
                Spacing = 14
            };

        content.Children.Add(
            CreateSection(
                "Privacy",

                new TextBlock
                {
                    Text =
                        "Axiom does not require an account.",

                    TextWrapping =
                        TextWrapping.Wrap
                },

                new TextBlock
                {
                    Text =
                        "Theme and effect packages remain data-only. Lua plugins are handled separately.",

                    TextWrapping =
                        TextWrapping.Wrap,

                    Classes =
                    {
                        "muted"
                    }
                }));


        SetPage(
            "Privacy",
            "Local data and extension behavior.",
            content);
    }

    private static Border CreateSection(
        string title,
        params Control[] controls)
    {
        var stack =
            new StackPanel
            {
                Spacing = 11
            };

        stack.Children.Add(
            new TextBlock
            {
                Text =
                    title,

                FontSize =
                    16,

                FontWeight =
                    FontWeight.SemiBold
            });

        foreach (var control in controls)
        {
            stack.Children.Add(
                control);
        }

        return new Border
        {
            Classes =
            {
                "card"
            },

            Child =
                stack
        };
    }

    private static Grid CreateRow(
        string title,
        Control control)
    {
        var grid =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "*,Auto")
            };

        var label =
            new TextBlock
            {
                Text =
                    title,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        Grid.SetColumn(
            control,
            1);

        grid.Children.Add(label);
        grid.Children.Add(control);

        return grid;
    }
}