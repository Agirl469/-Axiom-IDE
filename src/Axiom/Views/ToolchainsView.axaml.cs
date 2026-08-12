using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Axiom.Models;
using Axiom.Services;

namespace Axiom.Views;

public partial class ToolchainsView : UserControl
{
    private readonly ToolchainService _toolchains = new();
    private readonly PlatformService _platform = new();
    private readonly ProcessService _process = new();

    private readonly TextBlock _platformText;
    private readonly TextBlock _summaryText;
    private readonly StackPanel _toolchainList;
    private readonly WrapPanel _bundleList;

    private List<ToolchainStatus> _statuses = [];
    private string _filter = "all";
    private bool _refreshing;

    public ToolchainsView()
    {
        AvaloniaXamlLoader.Load(this);

        _platformText = this.FindControl<TextBlock>("PlatformText")
            ?? throw new InvalidOperationException("PlatformText was not found.");

        _summaryText = this.FindControl<TextBlock>("SummaryText")
            ?? throw new InvalidOperationException("SummaryText was not found.");

        _toolchainList = this.FindControl<StackPanel>("ToolchainList")
            ?? throw new InvalidOperationException("ToolchainList was not found.");

        _bundleList = this.FindControl<WrapPanel>("BundleList")
            ?? throw new InvalidOperationException("BundleList was not found.");

        _platformText.Text = BuildPlatformText();

        AttachedToVisualTree += async (_, _) => await RefreshAsync();
    }

    private string BuildPlatformText()
    {
        if (OperatingSystem.IsLinux())
        {
            var manager = _platform.LinuxPackageManager;
            return manager == "unknown"
                ? "Linux · package manager not detected"
                : $"Linux · {manager}";
        }

        if (OperatingSystem.IsWindows())
            return "Windows · winget";

        if (OperatingSystem.IsMacOS())
            return "macOS · manual setup";

        return _platform.PlatformName;
    }

    private async void Scan_Click(object? sender, RoutedEventArgs e) => await RefreshAsync();

    private void All_Click(object? sender, RoutedEventArgs e)
    {
        _filter = "all";
        RenderList();
    }

    private void Installed_Click(object? sender, RoutedEventArgs e)
    {
        _filter = "installed";
        RenderList();
    }

    private void Missing_Click(object? sender, RoutedEventArgs e)
    {
        _filter = "missing";
        RenderList();
    }

    private void Compilers_Click(object? sender, RoutedEventArgs e)
    {
        _filter = "compiler";
        RenderList();
    }

    private void BuildTools_Click(object? sender, RoutedEventArgs e)
    {
        _filter = "build";
        RenderList();
    }

    private async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        _toolchainList.Children.Clear();
        _bundleList.Children.Clear();

        _toolchainList.Children.Add(new TextBlock
        {
            Text = "Scanning toolchains...",
            Classes = { "muted" }
        });

        try
        {
            _statuses = await _toolchains.ScanAsync();
            RenderBundles();
            RenderList();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RenderBundles()
    {
        _bundleList.Children.Clear();

        foreach (var bundle in _toolchains.Bundles)
            _bundleList.Children.Add(CreateBundleCard(bundle));
    }

    private Control CreateBundleCard(ToolchainBundle bundle)
    {
        var statuses = _toolchains.GetBundleStatuses(bundle, _statuses);
        var installed = statuses.Count(x => x.Installed);
        var total = statuses.Count;
        var complete = total > 0 && installed == total;

        var title = new TextBlock
        {
            Text = bundle.Name,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        };

        var description = new TextBlock
        {
            Text = bundle.Description,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 238,
            Classes = { "muted" }
        };

        var progress = new TextBlock
        {
            Text = complete ? "Ready" : $"{installed} / {total} installed",
            Classes = { "muted" }
        };

        var button = new Button
        {
            Content = complete ? "Ready" : "Set up",
            IsEnabled = !complete,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 6)
        };

        button.Click += async (_, _) =>
            await InstallBundleAsync(bundle, button, progress);

        var header = new StackPanel
        {
            Spacing = 5,
            Children = { title, description }
        };

        var footer = new StackPanel
        {
            Spacing = 7,
            Children = { progress, button }
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            MinHeight = 120
        };

        Grid.SetRow(header, 0);
        Grid.SetRow(footer, 2);

        grid.Children.Add(header);
        grid.Children.Add(footer);

        return new Border
        {
            Classes = { "panel" },
            Width = 270,
            MinHeight = 150,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(14),
            Child = grid
        };
    }

    private async Task InstallBundleAsync(
        ToolchainBundle bundle,
        Button button,
        TextBlock statusText)
    {
        var statuses = _toolchains.GetBundleStatuses(bundle, _statuses);
        var missing = statuses.Where(x => !x.Installed).ToList();

        if (missing.Count == 0)
            return;

        button.IsEnabled = false;
        button.Content = "Setting up...";
        statusText.Text = "Installing required tools";

        if (OperatingSystem.IsWindows())
        {
            var result = await _toolchains.InstallBundleOnWindowsAsync(bundle, _statuses);

            if (result.ExitCode != 0)
            {
                button.Content = "Retry";
                button.IsEnabled = true;
                statusText.Text = "Setup failed";
                ToolTip.SetTip(button, result.Output);
                return;
            }

            await RefreshAsync();
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            var command = _toolchains.BuildBundleInstallCommand(bundle, _statuses);

            if (string.IsNullOrWhiteSpace(command))
            {
                button.Content = "Unavailable";
                statusText.Text = "No automatic installer";
                return;
            }

            if (!_process.TryRunCommandInTerminal(command))
            {
                button.Content = "Retry";
                button.IsEnabled = true;
                statusText.Text = "Could not open terminal";
                return;
            }

            button.Content = "Rescan";
            button.IsEnabled = true;
            statusText.Text = "Installer opened";
            return;
        }

        button.Content = "Unavailable";
        statusText.Text = "Manual setup required";
    }

    private void RenderList()
    {
        _toolchainList.Children.Clear();

        var installed = _statuses.Count(x => x.Installed);
        var missing = _statuses.Count - installed;
        _summaryText.Text = $"{installed} installed · {missing} missing";

        var visible = _statuses.Where(MatchesFilter).ToList();

        if (visible.Count == 0)
        {
            _toolchainList.Children.Add(new TextBlock
            {
                Text = "No toolchains match this filter.",
                Classes = { "muted" },
                Margin = new Thickness(4, 12)
            });
            return;
        }

        foreach (var status in visible)
            _toolchainList.Children.Add(CreateToolchainCard(status));
    }

    private bool MatchesFilter(ToolchainStatus status)
    {
        return _filter switch
        {
            "installed" => status.Installed,
            "missing" => !status.Installed,
            "compiler" => status.Toolchain.Category == "Compiler",
            "build" => status.Toolchain.Category == "Build Tool",
            _ => true
        };
    }

    private Control CreateToolchainCard(ToolchainStatus status)
    {
        var icon = new Border
        {
            Width = 54,
            Height = 54,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.Parse("#292B34")),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = status.Toolchain.ShortName,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var info = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = status.Toolchain.Name,
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = status.Toolchain.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { "muted" }
                },
                new TextBlock
                {
                    Text = status.Installed ? status.Version : "Not installed",
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { "muted" }
                }
            }
        };

        var statusText = new TextBlock
        {
            Text = status.Installed ? "Installed" : "Missing",
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var action = new Button
        {
            Content = status.Installed ? "Installed" : "Install",
            MinWidth = 92,
            Padding = new Thickness(14, 7),
            IsEnabled = !status.Installed &&
                        !string.IsNullOrWhiteSpace(status.InstallCommand)
        };

        action.Click += async (_, _) =>
            await InstallAsync(status, action, statusText);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
            Children = { statusText, action }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 14
        };

        Grid.SetColumn(icon, 0);
        Grid.SetColumn(info, 1);
        Grid.SetColumn(actions, 2);

        grid.Children.Add(icon);
        grid.Children.Add(info);
        grid.Children.Add(actions);

        return new Border
        {
            Classes = { "panel" },
            Padding = new Thickness(14),
            Child = grid
        };
    }

    private async Task InstallAsync(
        ToolchainStatus status,
        Button actionButton,
        TextBlock statusText)
    {
        if (status.Installed)
            return;

        if (string.IsNullOrWhiteSpace(status.InstallCommand))
        {
            statusText.Text = "No automatic installer";
            return;
        }

        actionButton.IsEnabled = false;
        actionButton.Content = "Installing...";
        statusText.Text = "Installing";

        if (OperatingSystem.IsWindows())
        {
            var result = await _toolchains.InstallOnWindowsAsync(status.Toolchain);

            if (result.ExitCode != 0)
            {
                actionButton.Content = "Retry";
                actionButton.IsEnabled = true;
                statusText.Text = "Install failed";
                ToolTip.SetTip(actionButton, result.Output);
                return;
            }

            await RefreshAsync();
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            if (!_process.TryRunCommandInTerminal(status.InstallCommand))
            {
                actionButton.Content = "Retry";
                actionButton.IsEnabled = true;
                statusText.Text = "Could not open terminal";
                return;
            }

            actionButton.Content = "Rescan";
            actionButton.IsEnabled = true;
            statusText.Text = "Installer opened";
            return;
        }

        actionButton.Content = "Unavailable";
        statusText.Text = "Manual install required";
    }
}