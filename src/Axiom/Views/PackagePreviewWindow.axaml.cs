using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Axiom.Packages;

namespace Axiom.Views;

public partial class PackagePreviewWindow : Window
{
    private readonly PackageValidationResult _validation;

    private readonly TextBlock _packageNameText;
    private readonly TextBlock _authorText;
    private readonly TextBlock _typeText;
    private readonly TextBlock _versionText;
    private readonly TextBlock _fileCountText;
    private readonly ListBox _validationList;
    private readonly Button _importButton;

    public PackagePreviewWindow(
        PackageValidationResult validation)
    {
        _validation =
            validation;

        AvaloniaXamlLoader.Load(this);

        _packageNameText =
            Get<TextBlock>(
                "PackageNameText");

        _authorText =
            Get<TextBlock>(
                "AuthorText");

        _typeText =
            Get<TextBlock>(
                "TypeText");

        _versionText =
            Get<TextBlock>(
                "VersionText");

        _fileCountText =
            Get<TextBlock>(
                "FileCountText");

        _validationList =
            Get<ListBox>(
                "ValidationList");

        _importButton =
            Get<Button>(
                "ImportButton");

        LoadPackage();
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

    private void LoadPackage()
    {
        var manifest =
            _validation.Manifest;

        if (manifest is null)
        {
            _packageNameText.Text =
                "Invalid Package";

            _importButton.IsEnabled =
                false;

            _validationList.ItemsSource =
                _validation.Errors;

            return;
        }

        _packageNameText.Text =
            manifest.Name;

        _authorText.Text =
            $"by {manifest.Author}";

        _typeText.Text =
            $"Type: {manifest.Type}";

        _versionText.Text =
            $"Version: {manifest.Version}";

        _fileCountText.Text =
            $"Files: {manifest.Files.Count}";

        var messages =
            new List<string>();

        if (_validation.IsValid)
        {
            messages.Add(
                "Package passed validation.");
        }

        messages.AddRange(
            _validation.Warnings);

        messages.AddRange(
            _validation.Errors);

        _validationList.ItemsSource =
            messages;

        _importButton.IsEnabled =
            _validation.IsValid;
    }

    private void Import_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}