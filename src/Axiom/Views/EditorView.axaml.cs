using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Axiom.Models;
using Axiom.Services;

namespace Axiom.Views;

public partial class EditorView : UserControl
{
    private readonly ProjectService _projects = new();
    private readonly ProcessService _process = new();
    private readonly string _root;
    private AxiomProject? _project;
    private string? _currentFile;

    public EditorView(string root)
    {
        _root = root;
        AvaloniaXamlLoader.Load(this);
        _ = LoadProjectAsync();
    }

    private async Task LoadProjectAsync()
    {
        _project = await _projects.LoadAsync(_root);
        var files = _projects.GetSourceFiles(_root).ToList();

        FileList.ItemsSource = files.Select(path => new ProjectFileItem(_root, path)).ToList();
        var title = _project?.Name ?? Path.GetFileName(_root);
        ProjectKindText.Text = $"{title} · {_projects.Describe(_project)}";
        OutputBox.Text = _project is null
            ? $"Opened {_root}{Environment.NewLine}No .axn project file found; this folder is in loose-file mode."
            : $"Opened {_root}{Environment.NewLine}{files.Count} files found.";
    }

    private async void FileList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is ProjectFileItem item)
            await OpenFileAsync(item.FullPath);
    }

    public void OpenFile(string path) => _ = OpenFileAsync(path);

    private async Task OpenFileAsync(string path)
    {
        try
        {
            _currentFile = path;
            CurrentFileText.Text = Path.GetRelativePath(_root, path);
            EditorBox.Text = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            OutputBox.Text = ex.Message;
        }
    }

    public async Task SaveCurrentFileAsync()
    {
        if (_currentFile is null)
            return;

        await File.WriteAllTextAsync(_currentFile, EditorBox.Text ?? string.Empty);
        OutputBox.Text = $"Saved {Path.GetRelativePath(_root, _currentFile)}";
    }

    private async void Build_Click(object? sender, RoutedEventArgs e) => await BuildProjectAsync();

    public async Task BuildProjectAsync()
    {
        if (_project is null)
        {
            OutputBox.Text = "This folder has no .axn project file, so Axiom does not know how to build it.";
            return;
        }

        OutputBox.Text = "Building...";

        try
        {
            var result = _project.Language switch
            {
                "cpp" => await BuildCppAsync(_project),
                "csharp" => await BuildCSharpAsync(_project),
                "rust" => await _process.RunAsync("cargo", "build", _root),
                "python" => (0, "Python projects do not need a compile step."),
                _ => (-1, $"No builder is registered for '{_project.Language}'.")
            };

            OutputBox.Text = $"Exit code: {result.Item1}{Environment.NewLine}{Environment.NewLine}{result.Item2}";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Build could not start.{Environment.NewLine}{Environment.NewLine}{ex.Message}";
        }
    }

    private async Task<(int, string)> BuildCppAsync(AxiomProject project)
    {
        var entry = project.Entry ?? "src/main.cpp";
        var buildDir = Path.Combine(_root, ".axiom", "build");
        Directory.CreateDirectory(buildDir);

        var outputName = OperatingSystem.IsWindows() ? project.Name + ".exe" : project.Name;
        var output = Path.Combine(buildDir, outputName);
        var compiler = project.Settings.GetValueOrDefault("compiler", "g++");
        var standard = project.Settings.GetValueOrDefault("standard", "c++20");
        var args = $"\"{entry}\" -std={standard} -o \"{output}\"";

        return await _process.RunAsync(compiler, args, _root);
    }

    private async Task<(int, string)> BuildCSharpAsync(AxiomProject project)
    {
        var axiomDir = Path.Combine(_root, ".axiom");
        var generatedDir = Path.Combine(axiomDir, "dotnet");
        Directory.CreateDirectory(generatedDir);

        var targetFramework = project.Settings.GetValueOrDefault("targetFramework", "net10.0");
        var generatedProject = Path.Combine(generatedDir, "build.csproj");
        var sourceGlob = Path.GetFullPath(Path.Combine(_root, "src", "**", "*.cs"));

        var xml = $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{targetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{sourceGlob}" />
  </ItemGroup>
</Project>
""";

        await File.WriteAllTextAsync(generatedProject, xml);
        return await _process.RunAsync("dotnet", $"build \"{generatedProject}\"", _root);
    }

    private sealed class ProjectFileItem
    {
        public ProjectFileItem(string root, string fullPath)
        {
            FullPath = fullPath;
            DisplayPath = Path.GetRelativePath(root, fullPath);
        }

        public string FullPath { get; }
        public string DisplayPath { get; }
        public override string ToString() => DisplayPath;
    }
}
