using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Axiom.Views;

namespace Axiom;

public partial class MainWindow : Window
{
    private StartPageView _startPage = null!;
    private ToolchainsView? _toolchains;

    public MainWindow()
    {
        InitializeComponent();

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
        PageHost.Content = _startPage;
    }

    private void ShowToolchains()
    {
        _toolchains ??= new ToolchainsView();

        PageHost.Content = _toolchains;
    }

    private void Toolchains_Click(object? sender, RoutedEventArgs e)
    {
        ShowToolchains();
    }

    private async void StartPage_NewProjectRequested(object? sender, EventArgs e)
    {
        await Task.CompletedTask;

        // New project window will go here.
    }

    private async void StartPage_OpenProjectRequested(object? sender, EventArgs e)
    {
        await Task.CompletedTask;

        // .axn project picker will go here.
    }

    private void StartPage_ToolchainsRequested(object? sender, EventArgs e)
    {
        ShowToolchains();
    }
}