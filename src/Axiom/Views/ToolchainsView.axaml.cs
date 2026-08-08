using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Axiom.Models;
using Axiom.Services;
using System;
namespace Axiom.Views;

public partial class ToolchainsView : UserControl
{
    private readonly ToolchainService _toolchains = new();
    private readonly PlatformService _platform = new();
    private readonly ProcessService _process = new();
    private readonly TextBlock _platformText;
    private readonly StackPanel _toolchainList;

    public ToolchainsView()
    {
        AvaloniaXamlLoader.Load(this);

        _platformText = this.FindControl<TextBlock>("PlatformText")
            ?? throw new InvalidOperationException(
                "PlatformText was not found in ToolchainsView.axaml.");

        _toolchainList = this.FindControl<StackPanel>("ToolchainList")
            ?? throw new InvalidOperationException(
                "ToolchainList was not found in ToolchainsView.axaml.");

        _platformText.Text =
            $"{_platform.PlatformName} · package manager: {_platform.LinuxPackageManager}";

        AttachedToVisualTree += async (_, _) =>
            await RefreshAsync();
    }

    private async void Scan_Click(object? sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _toolchainList.Children.Clear();

        _toolchainList.Children.Add(
            new TextBlock
            {
                Text = "Checking installed tools...",
                Classes = { "muted" }
            });

        var statuses = await _toolchains.ScanAsync();

        _toolchainList.Children.Clear();

        foreach (var status in statuses)
        {
            _toolchainList.Children.Add(
                CreateToolchainRow(status));
        }
    }

    private Control CreateToolchainRow(ToolchainStatus status)
    {
        var name = new TextBlock
        {
            Text = status.Toolchain.Name,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };

        var description = new TextBlock
        {
            Text = status.Toolchain.Description,
            Classes = { "muted" }
        };

        var version = new TextBlock
        {
            Text = status.Installed ? status.Version : "Not installed",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Classes = { "muted" }
        };

        var commandBox = new TextBox
        {
            Text = status.InstallCommand,
            IsReadOnly = true,
            MinWidth = 380,
            Watermark = "Install command"
        };

        var copy = new Button { Content = "Copy" };
        copy.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(status.InstallCommand);
        };

        var install = new Button
        {
            Content = status.Installed ? "Installed" : "Open terminal",
            IsEnabled = !status.Installed && !status.InstallCommand.StartsWith("Package manager") && !status.InstallCommand.StartsWith("No ")
        };
        install.Click += (_, _) => _process.TryStartTerminal(status.InstallCommand);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { commandBox, copy, install }
        };

        var details = new StackPanel
        {
            Spacing = 5,
            Children = { name, description, version, actions }
        };

        return new Border
        {
            Classes = { "panel" },
            Padding = new Thickness(16),
            Child = details
        };
    }
}
