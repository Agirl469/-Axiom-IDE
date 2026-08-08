using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Axiom.Models;
using Axiom.Services;

namespace Axiom.Views;

public partial class NewProjectWindow : Window
{
    private readonly TextBox _locationBox;
    private readonly TextBox _projectNameBox;
    private readonly ListBox _templateList;
    private readonly TextBlock _templateTitle;
    private readonly TextBlock _templateDescription;
    private readonly TextBlock _statusText;
    private string? _template;
    private readonly ProjectService _projects = new();

    public NewProjectWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _locationBox = this.FindControl<TextBox>("LocationBox")
            ?? throw new InvalidOperationException("LocationBox was not found.");

        _projectNameBox = this.FindControl<TextBox>("ProjectNameBox")
            ?? throw new InvalidOperationException("ProjectNameBox was not found.");

        _templateList = this.FindControl<ListBox>("TemplateList")
            ?? throw new InvalidOperationException("TemplateList was not found.");

        _templateTitle = this.FindControl<TextBlock>("TemplateTitle")
            ?? throw new InvalidOperationException("TemplateTitle was not found.");

        _templateDescription = this.FindControl<TextBlock>("TemplateDescription")
            ?? throw new InvalidOperationException("TemplateDescription was not found.");

        _statusText = this.FindControl<TextBlock>("StatusText")
            ?? throw new InvalidOperationException("StatusText was not found.");

        _locationBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Projects");

        _templateList.SelectedIndex = 0;
    }
    private void TemplateList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_templateList.SelectedItem is not ListBoxItem item)
            return;

        _template = item.Tag?.ToString();

        (_templateTitle.Text, _templateDescription.Text) = _template switch
        {
            "empty" => (
                "Empty project",
                "Creates a blank Axiom project with an .axn file and an empty src folder."
            ),

            "cpp" => (
                "C++ console",
                "A small C++ project that Axiom can build with GCC or Clang."
            ),

            "csharp" => (
                "C# console",
                "A .NET project managed by Axiom. No .sln is created for the project."
            ),

            "rust" => (
                "Rust binary",
                "A Rust project using Cargo as its build tool."
            ),

            "python" => (
                "Python project",
                "A basic Python project with no compile step."
            ),

            _ => (
                "Choose a template",
                "Select a project type on the left."
            )
        };
    }

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose project location",
            AllowMultiple = false
        });

        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
            LocationBox.Text = path;
    }

    private async void Create_Click(object? sender, RoutedEventArgs e)
    {
        if (_template is null)
        {
            _statusText.Text = "Choose a project template.";
            return;
        }

        var name = _projectNameBox.Text?.Trim();
        var location = _locationBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
        {
            _statusText.Text = "Project name and location are required.";
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _statusText.Text = "The project name contains invalid characters.";
            return;
        }

        var root = Path.Combine(location, name);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            _statusText.Text = "That folder already exists and is not empty.";
            return;
        }

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "src"));
        await WriteTemplateAsync(root, name, _template);
        Close(root);
    }

    private async Task WriteTemplateAsync(string root, string name, string template)
    {
        AxiomProject project;

        switch (template)
        {
            case "empty":
                project = new AxiomProject
                {
                    Name = name,
                    Language = "none",
                    Entry = null,
                    Settings = new Dictionary<string, string>()
                };
                break;
            case "cpp":
                project = new AxiomProject
                {
                    Name = name,
                    Language = "cpp",
                    Entry = "src/main.cpp",
                    Settings = new Dictionary<string, string>
                    {
                        ["compiler"] = "g++",
                        ["standard"] = "c++20"
                    }
                };
                await File.WriteAllTextAsync(Path.Combine(root, "src", "main.cpp"),
                    "#include <iostream>\n\nint main()\n{\n    std::cout << \"Hello from " + name + "\\n\";\n    return 0;\n}\n");
                break;

            case "csharp":
                project = new AxiomProject
                {
                    Name = name,
                    Language = "csharp",
                    Entry = "src/Program.cs",
                    Settings = new Dictionary<string, string>
                    {
                        ["targetFramework"] = "net10.0"
                    }
                };
                await File.WriteAllTextAsync(Path.Combine(root, "src", "Program.cs"),
                    "Console.WriteLine(\"Hello from " + name + "\");\n");
                break;

            case "rust":
                project = new AxiomProject
                {
                    Name = name,
                    Language = "rust",
                    Entry = "src/main.rs"
                };
                await File.WriteAllTextAsync(Path.Combine(root, "Cargo.toml"),
                    "[package]\nname = \"" + name.ToLowerInvariant().Replace(' ', '-') + "\"\nversion = \"0.1.0\"\nedition = \"2024\"\n\n[dependencies]\n");
                await File.WriteAllTextAsync(Path.Combine(root, "src", "main.rs"),
                    "fn main() {\n    println!(\"Hello from " + name + "\");\n}\n");
                break;

            case "python":
                project = new AxiomProject
                {
                    Name = name,
                    Language = "python",
                    Entry = "src/main.py"
                };
                await File.WriteAllTextAsync(Path.Combine(root, "src", "main.py"),
                    "def main():\n    print(\"Hello from " + name + "\")\n\n\nif __name__ == \"__main__\":\n    main()\n");
                break;

            default:
                throw new InvalidOperationException("Unknown project template.");
        }

        await _projects.SaveAsync(root, project);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
