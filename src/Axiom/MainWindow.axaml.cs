using System;
using System.IO;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using Axiom.Views;

namespace Axiom;

public partial class MainWindow : Window
{
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