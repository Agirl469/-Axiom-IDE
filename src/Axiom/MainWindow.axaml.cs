using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Axiom.Themes;
using Axiom.Views;
using System;
using System.IO;
namespace Axiom;

public partial class MainWindow : Window
{
    private readonly ThemeService _themes = new();
    private StartPageView _startPage = null!;
    private ToolchainsView? _toolchains;
    private EditorView? _editor;

    private readonly ContentControl _pageHost;
    private async void Menu_NewProject_Click(object? sender, RoutedEventArgs e)
    {
        var window = new NewProjectWindow();

        var projectRoot = await window.ShowDialog<string?>(this);

        if (!string.IsNullOrWhiteSpace(projectRoot))
            OpenRoot(projectRoot);
    }
    private async void Menu_OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open Axiom Project",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Axiom Project")
                {
                    Patterns = ["*.axn"]
                }
                ]
            });

        if (files.Count == 0)
            return;

        var projectFile = files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(projectFile))
            return;

        var root = Path.GetDirectoryName(projectFile);

        if (!string.IsNullOrWhiteSpace(root))
            OpenRoot(root);
    }
    private void ThemeDark_Click(
    object? sender,
    RoutedEventArgs e)
    {
        _themes.Apply(
            ThemeService.CreateAxiomDark());
    }

    private void ThemeLight_Click(
    object? sender,
    RoutedEventArgs e)
    {
        _themes.Apply(
            ThemeService.CreateLight());
    }
    private async void ImportTheme_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var input = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping =
                Avalonia.Media.TextWrapping.Wrap,

            Watermark =
                "Paste axiom-theme:// code here",

            MinHeight = 100
        };

        var status =
            new TextBlock
            {
                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            };

        var dialog =
            new Window
            {
                Title = "Import Theme",
                Width = 520,
                Height = 300,
                CanResize = false,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        var import =
            new Button
            {
                Content = "Import",
                MinWidth = 80
            };

        var cancel =
            new Button
            {
                Content = "Cancel",
                MinWidth = 80
            };

        import.Click += (_, _) =>
        {
            try
            {
                var theme =
                    ThemeShareCode.Decode(
                        input.Text ?? string.Empty);

                _themes.Apply(theme);

                dialog.Close();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        };

        cancel.Click += (_, _) =>
        {
            dialog.Close();
        };

        dialog.Content =
            new StackPanel
            {
                Margin =
                    new Avalonia.Thickness(18),

                Spacing = 10,

                Children =
                {
                new TextBlock
                {
                    Text =
                        "Paste an Axiom theme share code."
                },

                input,

                status,

                new StackPanel
                {
                    Orientation =
                        Avalonia.Layout.Orientation.Horizontal,

                    HorizontalAlignment =
                        Avalonia.Layout.HorizontalAlignment.Right,

                    Spacing = 8,

                    Children =
                    {
                        cancel,
                        import
                    }
                }
                }
            };

        await dialog.ShowDialog<object?>(this);
    }

    private async void CopyThemeCode_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var code =
            ThemeShareCode.Encode(
                _themes.Current);

        var clipboard =
            TopLevel.GetTopLevel(this)?
                .Clipboard;

        if (clipboard is not null)
            await clipboard.SetTextAsync(code);
    }

    private async void Menu_About_Click(object? sender, RoutedEventArgs e)
{
    var window = new Window
    {
        Title = "About Axiom",
        Width = 360,
        Height = 190,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,

        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 8,

            Children =
            {
                new TextBlock
                {
                    Text = "Axiom",
                    FontSize = 24,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },

                new TextBlock
                {
                    Text = "A lightweight cross-platform development environment."
                },

                new TextBlock
                {
                    Text = "Axiom projects use .axn."
                }
            }
        }
    };

    await window.ShowDialog<object?>(this);
}
    private async void Menu_Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_editor is not null)
            await _editor.SaveCurrentFileAsync();
    }

    private async void Menu_Build_Click(object? sender, RoutedEventArgs e)
    {
        if (_editor is not null)
            await _editor.BuildProjectAsync();
    }

    private async void Menu_Run_Click(object? sender, RoutedEventArgs e)
    {
        if (_editor is not null)
            await _editor.RunProjectAsync();
    }

    private void Menu_Undo_Click(object? sender, RoutedEventArgs e)
    {
        _editor?.Undo();
    }

    private void Menu_Redo_Click(object? sender, RoutedEventArgs e)
    {
        _editor?.Redo();
    }

    private void Menu_StartPage_Click(object? sender, RoutedEventArgs e)
    {
        ShowStartPage();
    }

    private void Menu_Toolchains_Click(object? sender, RoutedEventArgs e)
    {
        ShowToolchains();
    }

    private void Menu_Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _pageHost = this.FindControl<ContentControl>("PageHost")
            ?? throw new InvalidOperationException(
                "PageHost was not found in MainWindow.axaml.");

        CreateStartPage();
        ShowStartPage();
    }

    private void CreateStartPage()
    {
        _startPage = new StartPageView();

        _startPage.NewProjectRequested += StartPage_NewProjectRequested;
        _startPage.OpenProjectRequested += StartPage_OpenProjectRequested;
        _startPage.ToolchainsRequested += StartPage_ToolchainsRequested;
    }

    private void ShowStartPage()
    {
        _pageHost.Content = _startPage;
    }

    private void ShowToolchains()
    {
        _toolchains ??= new ToolchainsView();
        _pageHost.Content = _toolchains;
    }

    private void OpenRoot(string root)
    {
        _editor = new EditorView(root);
        _pageHost.Content = _editor;
    }

    private async void StartPage_NewProjectRequested(
        object? sender,
        EventArgs e)
    {
        var window = new NewProjectWindow();

        var projectRoot =
            await window.ShowDialog<string?>(this);

        if (string.IsNullOrWhiteSpace(projectRoot))
            return;

        OpenRoot(projectRoot);
    }

    private async void StartPage_OpenProjectRequested(
        object? sender,
        EventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open Axiom Project",
                AllowMultiple = false,

                FileTypeFilter =
                [
                    new FilePickerFileType("Axiom Project")
                    {
                        Patterns = ["*.axn"]
                    }
                ]
            });

        if (files.Count == 0)
            return;

        var projectFile =
            files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(projectFile))
            return;

        var projectRoot =
            Path.GetDirectoryName(projectFile);

        if (string.IsNullOrWhiteSpace(projectRoot))
            return;

        OpenRoot(projectRoot);
    }

    private void StartPage_ToolchainsRequested(
        object? sender,
        EventArgs e)
    {
        ShowToolchains();
    }

    private void Toolchains_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowToolchains();
    }

    private async void RunMenu_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_editor is not null)
            await _editor.RunProjectAsync();
    }
}