using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Axiom.Models;
using Axiom.Services;

namespace Axiom.Views;

public partial class NewProjectWindow : Window
{
    private string? _template;
    private readonly ProjectService _projects = new();

    public NewProjectWindow()
    {
        AvaloniaXamlLoader.Load(this);
        LocationBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
    }

    private void TemplateList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TemplateList.SelectedItem is not ListBoxItem item)
            return;

        _template = item.Tag?.ToString();

        (TemplateTitle.Text, TemplateDescription.Text) = _template switch
        {
            "cpp" => ("C++ console", "A small Axiom C++ project. Builds directly with GCC or Clang."),
            "csharp" => ("C# console", "An Axiom-managed .NET project. No .sln or visible .csproj is created."),
            "rust" => ("Rust binary", "An Axiom project using Cargo as its native Rust toolchain."),
            "python" => ("Python project", "A simple Python project with no compile step."),
            _ => ("Choose a template", "Select a project type on the left.")
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
            StatusText.Text = "Choose a project template.";
            return;
        }

        var name = ProjectNameBox.Text?.Trim();
        var location = LocationBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
        {
            StatusText.Text = "Project name and location are required.";
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            StatusText.Text = "The project name contains invalid characters.";
            return;
        }

        var root = Path.Combine(location, name);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            StatusText.Text = "That folder already exists and is not empty.";
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
