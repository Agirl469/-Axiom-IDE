using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Axiom.Services;

namespace Axiom.Views;

public partial class SlnImportWindow : Window
{
    private readonly SlnImportService _importer =
        new();

    private readonly string _solutionPath;

    private readonly ListBox _projectList;
    private readonly TextBlock _statusText;

    public SlnImportWindow(
        string solutionPath)
    {
        _solutionPath =
            solutionPath;

        AvaloniaXamlLoader.Load(this);

        _projectList =
            this.FindControl<ListBox>(
                "ProjectList")
            ?? throw new InvalidOperationException(
                "ProjectList was not found.");

        _statusText =
            this.FindControl<TextBlock>(
                "StatusText")
            ?? throw new InvalidOperationException(
                "StatusText was not found.");

        _ = LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects =
                await _importer
                    .ReadProjectsAsync(
                        _solutionPath);

            _projectList.ItemsSource =
                projects;

            if (projects.Count > 0)
                _projectList.SelectedIndex = 0;

            _statusText.Text =
                projects.Count == 1
                    ? "1 supported project found."
                    : $"{projects.Count} supported projects found.";
        }
        catch (Exception ex)
        {
            _statusText.Text =
                ex.Message;
        }
    }

    private async void Import_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_projectList.SelectedItem
            is not SlnProjectInfo project)
        {
            _statusText.Text =
                "Choose a project first.";

            return;
        }

        try
        {
            _statusText.Text =
                "Importing...";

            var root =
                await _importer
                    .ImportAsync(project);

            Close(root);
        }
        catch (Exception ex)
        {
            _statusText.Text =
                ex.Message;
        }
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}