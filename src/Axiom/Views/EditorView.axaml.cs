using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Axiom.Editor;
using Axiom.Models;
using Axiom.Services;
namespace Axiom.Views;


public partial class EditorView : UserControl
{
    private readonly ProjectService _projects = new();
    private readonly ProcessService _process = new();
    private readonly string _root;
    private AxiomProject? _project;

    private readonly TreeView _projectTree;
    private readonly TextBlock _projectKindText;
    private readonly StackPanel _tabBar;

    private readonly List<EditorTab> _tabs = new();
    private EditorTab? _activeTab;

    private bool _loadingEditorText;
    private readonly TextBox _editorBox;
    private readonly TextBox _outputBox;
    public EditorView(string root)
    {
        _root = root;

        AvaloniaXamlLoader.Load(this);

        _projectTree = this.FindControl<TreeView>("ProjectTree")
            ?? throw new InvalidOperationException(
                "ProjectTree was not found.");

        _projectKindText =
            this.FindControl<TextBlock>("ProjectKindText")
            ?? throw new InvalidOperationException(
                "ProjectKindText was not found.");

        _editorBox = this.FindControl<TextBox>("EditorBox")
            ?? throw new InvalidOperationException(
                "EditorBox was not found.");

        _outputBox = this.FindControl<TextBox>("OutputBox")
            ?? throw new InvalidOperationException(
                "OutputBox was not found.");

        _tabBar = this.FindControl<StackPanel>("TabBar")
            ?? throw new InvalidOperationException(
                "TabBar was not found.");

        _ = LoadProjectAsync();
    }
    public void Undo()
    {
        _editorBox.Undo();
    }

    public void Redo()
    {
        _editorBox.Redo();
    }

    private async Task LoadProjectAsync()
    {
        _project = await _projects.LoadAsync(_root);

        var files =
            _projects.GetSourceFiles(_root).ToList();

        RefreshProjectTree();

        var title =
            _project?.Name
            ?? Path.GetFileName(_root);

        _projectKindText.Text =
            $"{title} · {_projects.Describe(_project)}";

        _outputBox.Text =
            _project is null
                ? $"Opened {_root}{Environment.NewLine}" +
                  "No .axn project file found; this folder is in loose-file mode."
                : $"Opened {_root}{Environment.NewLine}" +
                  $"{files.Count} files found.";

        if (_project is not null &&
            !string.IsNullOrWhiteSpace(_project.Entry))
        {
            var entry =
                Path.Combine(
                    _root,
                    _project.Entry);

            if (File.Exists(entry))
                await OpenFileAsync(entry);
        }
    }
    private void EditorBox_KeyDown(
    object? sender,
    KeyEventArgs e)
    {
        if (_activeTab is null)
            return;

        if (e.Key == Key.Enter)
        {
            InsertIndentedNewLine();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                RemoveIndent();
            else
                InsertText(new string(
                    ' ',
                    IndentationService.TabSize));

            e.Handled = true;
        }
    }
    private void RefreshProjectTree()
    {
        var rootItem = CreateTreeItem(_root, true);

        _projectTree.ItemsSource =
            new[] { rootItem };
    }

    private TreeViewItem CreateTreeItem(
    string path,
    bool isRoot = false)
    {
        var isDirectory = Directory.Exists(path);

        var item = new TreeViewItem
        {
            Header = isRoot
                ? Path.GetFileName(
                    Path.GetFullPath(path)
                        .TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar))
                : Path.GetFileName(path),

            Tag = path,
            IsExpanded = isRoot
        };

        if (!isDirectory)
            return item;

        try
        {
            var entries =
                Directory
                    .EnumerateFileSystemEntries(path)
                    .Where(entry => !ShouldHideEntry(entry))
                    .OrderByDescending(Directory.Exists)
                    .ThenBy(
                        entry => Path.GetFileName(entry),
                        StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                item.Items.Add(
                    CreateTreeItem(entry));
            }
        }
        catch
        {
            // Leave unreadable directories empty.
        }

        return item;
    }

    private static bool ShouldHideEntry(string path)
    {
        var name = Path.GetFileName(path);

        return name is
            ".axiom" or
            ".git" or
            "bin" or
            "obj";
    }
    private string? GetSelectedPath()
    {
        if (_projectTree.SelectedItem
            is not TreeViewItem item)
        {
            return null;
        }

        return item.Tag as string;
    }
    private async void NewFile_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var directory =
            GetTargetDirectory();

        var name =
            await AskForNameAsync(
                "New File",
                "File name",
                "newfile.txt");

        if (string.IsNullOrWhiteSpace(name))
            return;

        if (Path.GetFileName(name) != name)
        {
            _outputBox.Text =
                "Enter a file name, not a full path.";

            return;
        }

        var path =
            Path.Combine(directory, name);

        if (File.Exists(path))
        {
            _outputBox.Text =
                $"'{name}' already exists.";

            return;
        }

        await File.WriteAllTextAsync(
            path,
            string.Empty);

        RefreshProjectTree();

        await OpenFileAsync(path);
    }
    private string GetTargetDirectory()
    {
        var selected = GetSelectedPath();

        if (string.IsNullOrWhiteSpace(selected))
            return _root;

        if (Directory.Exists(selected))
            return selected;

        return Path.GetDirectoryName(selected)
            ?? _root;
    }

    private async void DeleteItem_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var path = GetSelectedPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (Path.GetFullPath(path) ==
            Path.GetFullPath(_root))
        {
            _outputBox.Text =
                "The project root cannot be deleted from Axiom.";

            return;
        }

        var confirmed =
            await ConfirmAsync(
                $"Delete '{Path.GetFileName(path)}'?");

        if (!confirmed)
            return;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);

                var tab =
                    _tabs.FirstOrDefault(
                        item =>
                            string.Equals(
                                item.FilePath,
                                path,
                                StringComparison.OrdinalIgnoreCase));

                if (tab is not null)
                    await CloseDeletedTabAsync(tab);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(
                    path,
                    true);
            }

            RefreshProjectTree();
        }
        catch (Exception ex)
        {
            _outputBox.Text =
                $"Delete failed: {ex.Message}";
        }
    }

    private void RefreshTree_Click(
    object? sender,
    RoutedEventArgs e)
    {
        RefreshProjectTree();
    }


    private async Task<string?> AskForNameAsync(
    string title,
    string label,
    string initialValue)
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;

        if (owner is null)
            return null;

        var input = new TextBox
        {
            Text = initialValue,
            MinWidth = 320
        };

        var dialog =
            new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                CanResize = false,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        var create =
            new Button
            {
                Content = "OK",
                MinWidth = 75
            };

        var cancel =
            new Button
            {
                Content = "Cancel",
                MinWidth = 75
            };

        create.Click += (_, _) =>
        {
            dialog.Close(
                input.Text?.Trim());
        };

        cancel.Click += (_, _) =>
        {
            dialog.Close(null);
        };

        dialog.Content =
            new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 10,

                Children =
                {
                new TextBlock
                {
                    Text = label
                },

                input,

                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    Spacing = 8,

                    Children =
                    {
                        cancel,
                        create
                    }
                }
                }
            };

        return await dialog
            .ShowDialog<string?>(owner);
    }


    private async Task<bool> ConfirmAsync(
    string message)
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;

        if (owner is null)
            return false;

        var dialog =
            new Window
            {
                Title = "Axiom",
                Width = 380,
                Height = 160,
                CanResize = false,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        var yes =
            new Button
            {
                Content = "Delete",
                MinWidth = 80
            };

        var no =
            new Button
            {
                Content = "Cancel",
                MinWidth = 80
            };

        yes.Click += (_, _) =>
            dialog.Close(true);

        no.Click += (_, _) =>
            dialog.Close(false);

        dialog.Content =
            new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 14,

                Children =
                {
                new TextBlock
                {
                    Text = message,
                    TextWrapping =
                        Avalonia.Media.TextWrapping.Wrap
                },

                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    Spacing = 8,

                    Children =
                    {
                        no,
                        yes
                    }
                }
                }
            };

        return await dialog
            .ShowDialog<bool>(owner);
    }




    private async Task CloseDeletedTabAsync(
    EditorTab tab)
    {
        var index =
            _tabs.IndexOf(tab);

        _tabs.Remove(tab);

        if (ReferenceEquals(
            _activeTab,
            tab))
        {
            _activeTab = null;

            _loadingEditorText = true;
            _editorBox.Text = string.Empty;
            _loadingEditorText = false;

            if (_tabs.Count > 0)
            {
                var next =
                    Math.Clamp(
                        index,
                        0,
                        _tabs.Count - 1);

                ActivateTab(
                    _tabs[next]);
            }
        }

        RefreshTabBar();

        await Task.CompletedTask;
    }

    private async void RenameItem_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var oldPath = GetSelectedPath();

        if (string.IsNullOrWhiteSpace(oldPath))
            return;

        if (Path.GetFullPath(oldPath) ==
            Path.GetFullPath(_root))
        {
            _outputBox.Text =
                "The project root cannot be renamed from here.";

            return;
        }

        var oldName =
            Path.GetFileName(oldPath);

        var newName =
            await AskForNameAsync(
                "Rename",
                "New name",
                oldName);

        if (string.IsNullOrWhiteSpace(newName) ||
            newName == oldName)
        {
            return;
        }

        var parent =
            Path.GetDirectoryName(oldPath);

        if (parent is null)
            return;

        var newPath =
            Path.Combine(parent, newName);

        try
        {
            if (File.Exists(oldPath))
            {
                File.Move(
                    oldPath,
                    newPath);
            }
            else if (Directory.Exists(oldPath))
            {
                Directory.Move(
                    oldPath,
                    newPath);
            }

            RefreshProjectTree();
        }
        catch (Exception ex)
        {
            _outputBox.Text =
                $"Rename failed: {ex.Message}";
        }
    }
    private async void NewFolder_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var directory =
            GetTargetDirectory();

        var name =
            await AskForNameAsync(
                "New Folder",
                "Folder name",
                "NewFolder");

        if (string.IsNullOrWhiteSpace(name))
            return;

        if (Path.GetFileName(name) != name)
        {
            _outputBox.Text =
                "Enter a folder name, not a full path.";

            return;
        }

        var path =
            Path.Combine(directory, name);

        if (Directory.Exists(path))
        {
            _outputBox.Text =
                $"'{name}' already exists.";

            return;
        }

        Directory.CreateDirectory(path);

        RefreshProjectTree();
    }
    private async void ProjectTree_DoubleTapped(
    object? sender,
    TappedEventArgs e)
    {
        var path = GetSelectedPath();

        if (path is null)
            return;

        if (File.Exists(path))
            await OpenFileAsync(path);
    }
    public void OpenFile(string path) => _ = OpenFileAsync(path);

    private async Task OpenFileAsync(string path)
    {
        try
        {
            var existing = _tabs.FirstOrDefault(
                tab => string.Equals(
                    tab.FilePath,
                    path,
                    StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                ActivateTab(existing);
                return;
            }

            var text =
                await File.ReadAllTextAsync(path);

            var tab = new EditorTab
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                Language = LanguageService.FromFile(path),
                Text = text,
                IsDirty = false
            };

            _tabs.Add(tab);

            ActivateTab(tab);
            RefreshTabBar();
        }
        catch (Exception ex)
        {
            _outputBox.Text = ex.Message;
        }
    }


    private void ActivateTab(EditorTab tab)
    {
        StoreActiveTab();

        _activeTab = tab;

        _loadingEditorText = true;

        _editorBox.Text = tab.Text;

        _editorBox.CaretIndex =
            Math.Clamp(
                tab.CaretIndex,
                0,
                tab.Text.Length);

        _loadingEditorText = false;

        RefreshTabBar();

        _editorBox.Focus();
    }


    private void StoreActiveTab()
    {
        if (_activeTab is null)
            return;

        _activeTab.Text =
            _editorBox.Text
            ?? string.Empty;

        _activeTab.CaretIndex =
            _editorBox.CaretIndex;
    }

    private void RefreshTabBar()
    {
        _tabBar.Children.Clear();

        foreach (var tab in _tabs)
        {
            var openButton = new Button
            {
                Content = tab.DisplayName,
                Padding = new Thickness(10, 5),
                FontWeight =
                    ReferenceEquals(tab, _activeTab)
                        ? Avalonia.Media.FontWeight.SemiBold
                        : Avalonia.Media.FontWeight.Normal
            };

            openButton.Click += (_, _) =>
            {
                ActivateTab(tab);
            };

            var closeButton = new Button
            {
                Content = "×",
                Padding = new Thickness(7, 5)
            };

            closeButton.Click += async (_, _) =>
            {
                await CloseTabAsync(tab);
            };

            var tabGroup = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1
            };

            tabGroup.Children.Add(openButton);
            tabGroup.Children.Add(closeButton);

            _tabBar.Children.Add(tabGroup);
        }
    }


    private async Task CloseTabAsync(EditorTab tab)
    {
        if (ReferenceEquals(tab, _activeTab))
            StoreActiveTab();

        if (tab.IsDirty)
            await SaveTabAsync(tab);

        var index = _tabs.IndexOf(tab);

        _tabs.Remove(tab);

        if (ReferenceEquals(tab, _activeTab))
        {
            _activeTab = null;

            if (_tabs.Count > 0)
            {
                var nextIndex =
                    Math.Clamp(
                        index,
                        0,
                        _tabs.Count - 1);

                ActivateTab(_tabs[nextIndex]);
            }
            else
            {
                _loadingEditorText = true;

                _editorBox.Text = string.Empty;

                _loadingEditorText = false;
            }
        }

        RefreshTabBar();
    }

    private async Task SaveTabAsync(EditorTab tab)
    {
        await File.WriteAllTextAsync(
            tab.FilePath,
            tab.Text);

        tab.IsDirty = false;

        _outputBox.Text =
            $"Saved {Path.GetRelativePath(_root, tab.FilePath)}";

        RefreshTabBar();
    }
    public async Task SaveCurrentFileAsync()
    {
        if (_activeTab is null)
            return;

        StoreActiveTab();

        await SaveTabAsync(_activeTab);
    }
    private void EditorBox_TextChanged(
    object? sender,
    TextChangedEventArgs e)
    {
        if (_loadingEditorText ||
            _activeTab is null)
        {
            return;
        }

        _activeTab.Text =
            _editorBox.Text
            ?? string.Empty;

        if (!_activeTab.IsDirty)
        {
            _activeTab.IsDirty = true;
            RefreshTabBar();
        }
    }
    private void InsertIndentedNewLine()
    {
        var text =
            _editorBox.Text
            ?? string.Empty;

        var caret =
            _editorBox.CaretIndex;

        var lineStart =
            text.LastIndexOf(
                '\n',
                Math.Max(0, caret - 1));

        lineStart =
            lineStart < 0
                ? 0
                : lineStart + 1;

        var currentLine =
            text.Substring(
                lineStart,
                caret - lineStart);

        var indent =
            IndentationService.GetIndentForNewLine(
                _activeTab!.Language,
                currentLine);

        InsertText(
            Environment.NewLine + indent);
    }
    private void RemoveIndent()
    {
        var text =
            _editorBox.Text
            ?? string.Empty;

        var caret =
            _editorBox.CaretIndex;

        if (caret == 0)
            return;

        var lineStart =
            text.LastIndexOf(
                '\n',
                Math.Max(0, caret - 1));

        lineStart =
            lineStart < 0
                ? 0
                : lineStart + 1;

        var removeCount = 0;

        while (
            removeCount < IndentationService.TabSize &&
            lineStart + removeCount < text.Length &&
            text[lineStart + removeCount] == ' ')
        {
            removeCount++;
        }

        if (removeCount == 0)
            return;

        _editorBox.Text =
            text.Remove(
                lineStart,
                removeCount);

        _editorBox.CaretIndex =
            Math.Max(
                lineStart,
                caret - removeCount);
    }
    private void InsertText(string value)
    {
        var text =
            _editorBox.Text
            ?? string.Empty;

        var start =
            Math.Min(
                _editorBox.SelectionStart,
                _editorBox.SelectionEnd);

        var end =
            Math.Max(
                _editorBox.SelectionStart,
                _editorBox.SelectionEnd);

        var newText =
            text[..start] +
            value +
            text[end..];

        _editorBox.Text = newText;

        _editorBox.CaretIndex =
            start + value.Length;
    }
    private async void Build_Click(object? sender, RoutedEventArgs e) => await BuildProjectAsync();
    private async void Run_Click(object? sender, RoutedEventArgs e)
    {
        await RunProjectAsync();
    }

    public async Task RunProjectAsync()
    {
        if (_project is null)
        {
            _outputBox.Text =
                "This folder has no .axn project file, so Axiom does not know how to run it.";
            return;
        }

        await SaveCurrentFileAsync();

        _outputBox.Text = "Running...";

        try
        {
            var result = _project.Language switch
            {
                "cpp" => await RunCppAsync(_project),

                "csharp" => await RunCSharpAsync(_project),

                "rust" => await _process.RunAsync(
                    "cargo",
                    "run",
                    _root),

                "python" => await _process.RunAsync(
                    OperatingSystem.IsWindows() ? "python" : "python3",
                    $"\"{_project.Entry ?? "src/main.py"}\"",
                    _root),

                "none" => (-1, "Empty projects do not have anything to run."),

                _ => (-1, $"No runner is registered for '{_project.Language}'.")
            };

            _outputBox.Text =
                $"Exit code: {result.Item1}" +
                Environment.NewLine +
                Environment.NewLine +
                result.Item2;
        }
        catch (Exception ex)
        {
            _outputBox.Text =
                "Run could not start." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message;
        }
    }

    private async Task<(int, string)> RunCppAsync(AxiomProject project)
    {
        var build = await BuildCppAsync(project);

        if (build.Item1 != 0)
            return build;

        var outputName =
            OperatingSystem.IsWindows()
                ? project.Name + ".exe"
                : project.Name;

        var executable = Path.Combine(
            _root,
            ".axiom",
            "build",
            outputName);

        return await _process.RunAsync(
            executable,
            "",
            _root);
    }

    private async Task<(int, string)> RunCSharpAsync(AxiomProject project)
    {
        var axiomDir = Path.Combine(_root, ".axiom");
        var generatedDir = Path.Combine(axiomDir, "dotnet");

        Directory.CreateDirectory(generatedDir);

        var targetFramework =
            project.Settings.GetValueOrDefault(
                "targetFramework",
                "net10.0");

        var generatedProject =
            Path.Combine(generatedDir, "build.csproj");

        var sourceGlob =
            Path.GetFullPath(
                Path.Combine(
                    _root,
                    "src",
                    "**",
                    "*.cs"));

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

        await File.WriteAllTextAsync(
            generatedProject,
            xml);

        return await _process.RunAsync(
            "dotnet",
            $"run --project \"{generatedProject}\"",
            _root);
    }
    public async Task BuildProjectAsync()
    {
        if (_project is null)
        {
            _outputBox.Text = "This folder has no .axn project file, so Axiom does not know how to build it.";
            return;
        }

        _outputBox.Text = "Building...";

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

            _outputBox.Text = $"Exit code: {result.Item1}{Environment.NewLine}{Environment.NewLine}{result.Item2}";
        }
        catch (Exception ex)
        {
            _outputBox.Text = $"Build could not start.{Environment.NewLine}{Environment.NewLine}{ex.Message}";
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
}

 
