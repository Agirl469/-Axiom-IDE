using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Axiom.Views;

public partial class StartPageView : UserControl
{
    public event EventHandler? NewProjectRequested;
    public event EventHandler? OpenProjectRequested;
    public event EventHandler? ToolchainsRequested;

    public StartPageView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        NewProjectRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        OpenProjectRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Toolchains_Click(object? sender, RoutedEventArgs e)
    {
        ToolchainsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow main)
            main.FindControl<ContentControl>("PageHost")!.Content = new SettingsView();
    }
}
