using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Axiom.Build;
using Axiom.Editor;
using Axiom.Models;
using Axiom.Services;

namespace Axiom.Views;

public partial class EditorView : UserControl
{
    private readonly ProjectService _projects = new();
    private readonly ProcessService _process = new();
    private readonly KeybindService _keybinds = new();
    private readonly string _root;

    private AxiomProject? _project;
    private EditorTab? _activeTab;

    private readonly TreeView _projectTree;
    private readonly TextBlock _projectKindText;
    private readonly StackPanel _tabBar;
    private readonly TextBox _editorBox;

    private readonly TextBox _outputBox;
    private readonly ListBox _problemsList;

    private readonly Grid _terminalPanel;
    private readonly TextBox _terminalOutput;
    private readonly TextBox _terminalInput;

    private readonly List<EditorTab> _tabs = new();
    private readonly List<BuildProblem> _problems = new();

    private bool _loadingEditorText;
    private CancellationTokenSource? _executionCancellation;

    public EditorView(string root)
    {
        _root = root;

        AvaloniaXamlLoader.Load(this);

        _projectTree =
            this.FindControl<TreeView>("ProjectTree")
            ?? throw new InvalidOperationException(
                "ProjectTree was not found.");

        _projectKindText =
            this.FindControl<TextBlock>("ProjectKindText")
            ?? throw new InvalidOperationException(
                "ProjectKindText was not found.");

        _tabBar =
            this.FindControl<StackPanel>("TabBar")
            ?? throw new InvalidOperationException(
                "TabBar was not found.");

        _editorBox =
            this.FindControl<TextBox>("EditorBox")
            ?? throw new InvalidOperationException(
                "EditorBox was not found.");

        _outputBox =
            this.FindControl<TextBox>("OutputBox")
            ?? throw new InvalidOperationException(
                "OutputBox was not found.");

        _problemsList =
            this.FindControl<ListBox>("ProblemsList")
            ?? throw new InvalidOperationException(
                "ProblemsList was not found.");

        _terminalPanel =
            this.FindControl<Grid>("TerminalPanel")
            ?? throw new InvalidOperationException(
                "TerminalPanel was not found.");

        _terminalOutput =
            this.FindControl<TextBox>("TerminalOutput")
            ?? throw new InvalidOperationException(
                "TerminalOutput was not found.");

        _terminalInput =
            this.FindControl<TextBox>("TerminalInput")
            ?? throw new InvalidOperationException(
                "TerminalInput was not found.");

        ShowBottomPanel("output");

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
        try
        {
            _project =
                await _projects.LoadAsync(_root);

            RefreshProjectTree();

            var files =
                _projects
                    .GetSourceFiles(_root)
                    .ToList();

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
        catch (Exception ex)
        {
            _outputBox.Text =
                $"Could not load project.{Environment.NewLine}{ex.Message}";
        }
    }

    private void OutputTab_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowBottomPanel("output");
    }

    private void ProblemsTab_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowBottomPanel("problems");
    }

    private void TerminalTab_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowBottomPanel("terminal");
        _terminalInput.Focus();
    }

    private void ShowBottomPanel(string panel)
    {
        _outputBox.IsVisible =
            panel == "output";

        _problemsList.IsVisible =
            panel == "problems";

        _terminalPanel.IsVisible =
            panel == "terminal";
    }

    private void RefreshProjectTree()
    {
        var rootItem =
            CreateTreeItem(
                _root,
                true);

        _projectTree.ItemsSource =
            new[]
            {
                rootItem
            };
    }

    private TreeViewItem CreateTreeItem(
        string path,
        bool isRoot = false)
    {
        var isDirectory =
            Directory.Exists(path);

        var name =
            isRoot
                ? Path.GetFileName(
                    Path.GetFullPath(path)
                        .TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar))
                : Path.GetFileName(path);

        if (string.IsNullOrWhiteSpace(name))
            name = path;

        var item =
            new TreeViewItem
            {
                Header = name,
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
                    .Where(entry =>
                        !ShouldHideEntry(entry))
                    .OrderByDescending(
                        Directory.Exists)
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
        }

        return item;
    }

    private static bool ShouldHideEntry(string path)
    {
        var name =
            Path.GetFileName(path);

        return name is
            ".git" or
            ".axiom" or
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

    private string GetTargetDirectory()
    {
        var selected =
            GetSelectedPath();

        if (string.IsNullOrWhiteSpace(selected))
            return _root;

        if (Directory.Exists(selected))
            return selected;

        return Path.GetDirectoryName(selected)
            ?? _root;
    }

    private async void ProjectTree_DoubleTapped(
        object? sender,
        TappedEventArgs e)
    {
        var path =
            GetSelectedPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (File.Exists(path))
            await OpenFileAsync(path);
    }

    private void RefreshTree_Click(
        object? sender,
        RoutedEventArgs e)
    {
        RefreshProjectTree();
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
            Path.Combine(
                directory,
                name);

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
            Path.Combine(
                directory,
                name);

        if (Directory.Exists(path))
        {
            _outputBox.Text =
                $"'{name}' already exists.";

            return;
        }

        Directory.CreateDirectory(path);

        RefreshProjectTree();
    }

    private async void RenameItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var oldPath =
            GetSelectedPath();

        if (string.IsNullOrWhiteSpace(oldPath))
            return;

        if (PathsEqual(oldPath, _root))
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
            Path.Combine(
                parent,
                newName);

        try
        {
            if (File.Exists(oldPath))
            {
                File.Move(
                    oldPath,
                    newPath);

                var tab =
                    _tabs.FirstOrDefault(
                        item =>
                            PathsEqual(
                                item.FilePath,
                                oldPath));

                if (tab is not null)
                {
                    var wasActive =
                        ReferenceEquals(
                            _activeTab,
                            tab);

                    if (wasActive)
                        StoreActiveTab();

                    _tabs.Remove(tab);

                    if (wasActive)
                        _activeTab = null;

                    RefreshTabBar();

                    await OpenFileAsync(newPath);
                }
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

    private async void DeleteItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var path =
            GetSelectedPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (PathsEqual(path, _root))
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
                var tab =
                    _tabs.FirstOrDefault(
                        item =>
                            PathsEqual(
                                item.FilePath,
                                path));

                File.Delete(path);

                if (tab is not null)
                    CloseDeletedTab(tab);
            }
            else if (Directory.Exists(path))
            {
                var affectedTabs =
                    _tabs
                        .Where(tab =>
                            IsInsideDirectory(
                                tab.FilePath,
                                path))
                        .ToList();

                Directory.Delete(
                    path,
                    true);

                foreach (var tab in affectedTabs)
                    CloseDeletedTab(tab);
            }

            RefreshProjectTree();
        }
        catch (Exception ex)
        {
            _outputBox.Text =
                $"Delete failed: {ex.Message}";
        }
    }

    private void CloseDeletedTab(EditorTab tab)
    {
        var index =
            _tabs.IndexOf(tab);

        var wasActive =
            ReferenceEquals(
                _activeTab,
                tab);

        _tabs.Remove(tab);

        if (wasActive)
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
    }

    public void OpenFile(string path)
    {
        _ = OpenFileAsync(path);
    }

    private async Task OpenFileAsync(string path)
    {
        try
        {
            var existing =
                _tabs.FirstOrDefault(
                    tab =>
                        PathsEqual(
                            tab.FilePath,
                            path));

            if (existing is not null)
            {
                ActivateTab(existing);
                return;
            }

            var text =
                await File.ReadAllTextAsync(path);

            var tab =
                new EditorTab
                {
                    FilePath = path,
                    FileName =
                        Path.GetFileName(path),

                    Language =
                        LanguageService.FromFile(path),

                    Text = text,
                    IsDirty = false
                };

            _tabs.Add(tab);

            ActivateTab(tab);
            RefreshTabBar();
        }
        catch (Exception ex)
        {
            _outputBox.Text =
                ex.Message;
        }
    }

    private void ActivateTab(EditorTab tab)
    {
        StoreActiveTab();

        _activeTab = tab;
        _loadingEditorText = true;

        _editorBox.Text =
            tab.Text;

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
            var openButton =
                new Button
                {
                    Content = tab.DisplayName,

                    Padding =
                        new Thickness(
                            10,
                            5),

                    FontWeight =
                        ReferenceEquals(
                            tab,
                            _activeTab)
                            ? Avalonia.Media.FontWeight.SemiBold
                            : Avalonia.Media.FontWeight.Normal
                };

            openButton.Click +=
                (_, _) =>
                {
                    ActivateTab(tab);
                };

            var closeButton =
                new Button
                {
                    Content = "×",

                    Padding =
                        new Thickness(
                            7,
                            5)
                };

            closeButton.Click +=
                async (_, _) =>
                {
                    await CloseTabAsync(tab);
                };

            var group =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    Spacing = 1
                };

            group.Children.Add(
                openButton);

            group.Children.Add(
                closeButton);

            _tabBar.Children.Add(
                group);
        }
    }

    private async Task CloseTabAsync(EditorTab tab)
    {
        if (ReferenceEquals(
                tab,
                _activeTab))
        {
            StoreActiveTab();
        }

        if (tab.IsDirty)
            await SaveTabAsync(tab);

        var index =
            _tabs.IndexOf(tab);

        var wasActive =
            ReferenceEquals(
                tab,
                _activeTab);

        _tabs.Remove(tab);

        if (wasActive)
        {
            _activeTab = null;

            if (_tabs.Count > 0)
            {
                var nextIndex =
                    Math.Clamp(
                        index,
                        0,
                        _tabs.Count - 1);

                ActivateTab(
                    _tabs[nextIndex]);
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

    public async Task SaveCurrentFileAsync()
    {
        if (_activeTab is null)
            return;

        StoreActiveTab();

        await SaveTabAsync(
            _activeTab);
    }

    private async Task SaveTabAsync(EditorTab tab)
    {
        await File.WriteAllTextAsync(
            tab.FilePath,
            tab.Text);

        tab.IsDirty = false;

        RefreshTabBar();
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



    private void StopExecution()
    {
        if (_executionCancellation is null)
            return;

        if (_executionCancellation.IsCancellationRequested)
            return;

        _executionCancellation.Cancel();

        AppendOutput("Stopping...");
    }

    private void Stop_Click(
        object? sender,
        RoutedEventArgs e)
    {
        StopExecution();
    }
    private async void EditorBox_KeyDown(
    object? sender,
    KeyEventArgs e)
    {
        if (_keybinds.Matches(
            e,
            _keybinds.Settings.Save))
        {
            e.Handled = true;

            await SaveCurrentFileAsync();

            return;
        }

        if (_keybinds.Matches(
            e,
            _keybinds.Settings.Build))
        {
            e.Handled = true;

            await BuildProjectAsync();

            return;
        }

        if (_keybinds.Matches(
            e,
            _keybinds.Settings.Run))
        {
            e.Handled = true;

            await RunProjectAsync();

            return;
        }

        if (_keybinds.Matches(
            e,
            _keybinds.Settings.Stop))
        {
            e.Handled = true;

            StopExecution();

            return;
        }

        if (_keybinds.Matches(
            e,
            _keybinds.Settings.Terminal))
        {
            e.Handled = true;

            ShowBottomPanel("terminal");
            _terminalInput.Focus();

            return;
        }

        if (_keybinds.Matches(
            e,
            _keybinds.Settings.CloseTab))
        {
            e.Handled = true;

            if (_activeTab is not null)
                await CloseTabAsync(_activeTab);

            return;
        }

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
                InsertText(new string(' ', IndentationService.TabSize));

            e.Handled = true;
        }
    }

    private void InsertIndentedNewLine()
    {
        if (_activeTab is null)
            return;

        var text =
            _editorBox.Text
            ?? string.Empty;

        var caret =
            _editorBox.CaretIndex;

        var searchIndex =
            Math.Max(
                0,
                caret - 1);

        var lineStart =
            caret == 0
                ? -1
                : text.LastIndexOf(
                    '\n',
                    searchIndex);

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
                _activeTab.Language,
                currentLine);

        InsertText(
            Environment.NewLine +
            indent);
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

        _editorBox.Text =
            newText;

        _editorBox.CaretIndex =
            start + value.Length;
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
                Math.Max(
                    0,
                    caret - 1));

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

    private async void Build_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await BuildProjectAsync();
    }

    private async void Run_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await RunProjectAsync();
    }

 

    private void Terminal_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowBottomPanel("terminal");
        _terminalInput.Focus();
    }

    private void BeginExecution()
    {
        _executionCancellation?.Cancel();
        _executionCancellation?.Dispose();

        _executionCancellation =
            new CancellationTokenSource();
    }

    public async Task RunProjectAsync()
    {
        if (_project is null)
        {
            _outputBox.Text =
                "This folder has no .axn project file, so Axiom does not know how to run it.";

            return;
        }

        BeginExecution();

        await SaveCurrentFileAsync();

        _outputBox.Text =
            "Running...";

        ShowBottomPanel("output");

        try
        {
            ProcessResult result =
                _project.Language switch
                {
                    "cpp" =>
                        await RunCppAsync(
                            _project),

                    "csharp" =>
                        await RunCSharpAsync(
                            _project),

                    "rust" =>
                        await RunProcessAsync(
                            "cargo",
                            "run"),

                    "python" =>
                        await RunProcessAsync(
                            OperatingSystem.IsWindows()
                                ? "python"
                                : "python3",

                            $"\"{_project.Entry ?? "src/main.py"}\""),

                    "none" =>
                        new ProcessResult(
                            -1,
                            "Empty projects do not have anything to run."),

                    _ =>
                        new ProcessResult(
                            -1,
                            $"No runner is registered for '{_project.Language}'.")
                };

            AppendExecutionSummary(
                result);
        }
        catch (Exception ex)
        {
            AppendOutput(
                $"Run could not start: {ex.Message}");
        }
    }

    public async Task BuildProjectAsync()
    {
        if (_project is null)
        {
            _outputBox.Text =
                "This folder has no .axn project file, so Axiom does not know how to build it.";

            return;
        }

        BeginExecution();

        _problems.Clear();
        _problemsList.ItemsSource = null;

        await SaveCurrentFileAsync();

        _outputBox.Text =
            "Building...";

        ShowBottomPanel("output");

        try
        {
            ProcessResult result =
                _project.Language switch
                {
                    "cpp" =>
                        await BuildCppAsync(
                            _project),

                    "csharp" =>
                        await BuildCSharpAsync(
                            _project),

                    "rust" =>
                        await RunProcessAsync(
                            "cargo",
                            "build"),

                    "python" =>
                        new ProcessResult(
                            0,
                            "Python projects do not need a compile step."),

                    "none" =>
                        new ProcessResult(
                            -1,
                            "Empty projects do not have anything to build."),

                    _ =>
                        new ProcessResult(
                            -1,
                            $"No builder is registered for '{_project.Language}'.")
                };

            if (_project.Language == "python")
            {
                AppendOutput(
                    result.Output);
            }

            AppendExecutionSummary(
                result);

            if (result.ExitCode != 0 &&
                _problems.Count > 0)
            {
                ShowBottomPanel(
                    "problems");
            }
        }
        catch (Exception ex)
        {
            AppendOutput(
                $"Build could not start: {ex.Message}");
        }
    }

    private async Task<ProcessResult> RunCppAsync(
        AxiomProject project)
    {
        var build =
            await BuildCppAsync(project);

        if (build.ExitCode != 0)
            return build;

        var outputName =
            OperatingSystem.IsWindows()
                ? project.Name + ".exe"
                : project.Name;

        var executable =
            Path.Combine(
                _root,
                ".axiom",
                "build",
                outputName);

        return await RunProcessAsync(
            executable,
            string.Empty);
    }

    private async Task<ProcessResult> RunCSharpAsync(
        AxiomProject project)
    {
        var generatedProject =
            await CreateDotNetBuildProjectAsync(
                project);

        return await RunProcessAsync(
            "dotnet",
            $"run --project \"{generatedProject}\"");
    }

    private async Task<ProcessResult> BuildCppAsync(
        AxiomProject project)
    {
        var entry =
            project.Entry
            ?? "src/main.cpp";

        var buildDir =
            Path.Combine(
                _root,
                ".axiom",
                "build");

        Directory.CreateDirectory(
            buildDir);

        var outputName =
            OperatingSystem.IsWindows()
                ? project.Name + ".exe"
                : project.Name;

        var output =
            Path.Combine(
                buildDir,
                outputName);

        var compiler =
            project.Settings.GetValueOrDefault(
                "compiler",
                "g++");

        var standard =
            project.Settings.GetValueOrDefault(
                "standard",
                "c++20");

        var args =
            $"\"{entry}\" -std={standard} -o \"{output}\"";

        return await RunProcessAsync(
            compiler,
            args);
    }

    private async Task<ProcessResult> BuildCSharpAsync(
        AxiomProject project)
    {
        var generatedProject =
            await CreateDotNetBuildProjectAsync(
                project);

        return await RunProcessAsync(
            "dotnet",
            $"build \"{generatedProject}\"");
    }

    private async Task<string> CreateDotNetBuildProjectAsync(
        AxiomProject project)
    {
        var axiomDir =
            Path.Combine(
                _root,
                ".axiom");

        var generatedDir =
            Path.Combine(
                axiomDir,
                "dotnet");

        Directory.CreateDirectory(
            generatedDir);

        var targetFramework =
            project.Settings.GetValueOrDefault(
                "targetFramework",
                "net10.0");

        var generatedProject =
            Path.Combine(
                generatedDir,
                "build.csproj");

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

        return generatedProject;
    }

    private Task<ProcessResult> RunProcessAsync(
        string executable,
        string arguments)
    {
        return _process.RunAsync(
            executable,
            arguments,
            _root,
            line => AppendOutput(line),
            _executionCancellation?.Token
                ?? CancellationToken.None);
    }

    private void AppendOutput(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var problem =
            BuildProblemParser.Parse(line);

        Dispatcher.UIThread.Post(() =>
        {
            if (problem is not null)
            {
                _problems.Add(problem);

                _problemsList.ItemsSource = null;

                _problemsList.ItemsSource =
                    _problems.ToList();
            }

            if (string.IsNullOrWhiteSpace(
                    _outputBox.Text))
            {
                _outputBox.Text = line;
            }
            else
            {
                _outputBox.Text +=
                    Environment.NewLine +
                    line;
            }

            _outputBox.CaretIndex =
                _outputBox.Text?.Length
                ?? 0;
        });
    }

    private void AppendExecutionSummary(ProcessResult result)
    {
        if (result.ExitCode == -1)
        {
            var message =
                string.IsNullOrWhiteSpace(result.Output)
                    ? "The process could not be started."
                    : result.Output.Trim();

            AppendOutput(message);

            if (_problems.Count == 0)
            {
                _problems.Add(
                    new BuildProblem
                    {
                        Severity = ProblemSeverity.Error,
                        FilePath = string.Empty,
                        Line = 0,
                        Column = 0,
                        Message = message
                    });

                _problemsList.ItemsSource = null;
                _problemsList.ItemsSource = _problems.ToList();
            }

            AppendOutput("Process could not be started.");
            return;
        }

        AppendOutput(
            $"Process exited with code {result.ExitCode}.");

        if (_problems.Count > 0)
        {
            AppendOutput(
                $"{_problems.Count} build problem(s) detected.");
        }
    }

    private async void ProblemsList_DoubleTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (_problemsList.SelectedItem
            is not BuildProblem problem)
        {
            return;
        }

        var path =
            problem.FilePath;

        if (!Path.IsPathRooted(path))
        {
            path =
                Path.Combine(
                    _root,
                    path);
        }

        if (!File.Exists(path))
            return;

        await OpenFileAsync(path);

        if (problem.Line > 0)
        {
            JumpToLine(
                problem.Line,
                problem.Column);
        }
    }

    private void JumpToLine(
        int line,
        int column)
    {
        var text =
            _editorBox.Text
            ?? string.Empty;

        var currentLine = 1;
        var index = 0;

        while (
            currentLine < line &&
            index < text.Length)
        {
            if (text[index] == '\n')
                currentLine++;

            index++;
        }

        if (column > 1)
            index += column - 1;

        index =
            Math.Clamp(
                index,
                0,
                text.Length);

        _editorBox.CaretIndex =
            index;

        _editorBox.SelectionStart =
            index;

        _editorBox.SelectionEnd =
            index;

        _editorBox.Focus();
    }

    private async void TerminalInput_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        var command =
            _terminalInput.Text?.Trim();

        if (string.IsNullOrWhiteSpace(command))
            return;

        _terminalInput.Text =
            string.Empty;

        AppendTerminal(
            $"> {command}");

        if (command.Equals(
                "clear",
                StringComparison.OrdinalIgnoreCase) ||
            command.Equals(
                "cls",
                StringComparison.OrdinalIgnoreCase))
        {
            _terminalOutput.Text =
                string.Empty;

            return;
        }

        ProcessResult result;

        if (OperatingSystem.IsWindows())
        {
            result =
                await _process.RunAsync(
                    "cmd.exe",
                    $"/c {command}",
                    _root);
        }
        else
        {
            result =
                await _process.RunAsync(
                    "/bin/sh",
                    $"-c \"{EscapeShellCommand(command)}\"",
                    _root);
        }

        if (!string.IsNullOrWhiteSpace(
                result.Output))
        {
            AppendTerminal(
                result.Output.TrimEnd());
        }

        if (result.ExitCode != 0)
        {
            AppendTerminal(
                $"Exit code: {result.ExitCode}");
        }
    }

    private void AppendTerminal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (string.IsNullOrWhiteSpace(
                _terminalOutput.Text))
        {
            _terminalOutput.Text =
                text;
        }
        else
        {
            _terminalOutput.Text +=
                Environment.NewLine +
                text;
        }

        _terminalOutput.CaretIndex =
            _terminalOutput.Text?.Length
            ?? 0;
    }

    private static string EscapeShellCommand(
        string command)
    {
        return command
            .Replace(
                "\\",
                "\\\\")
            .Replace(
                "\"",
                "\\\"");
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

        var input =
            new TextBox
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

        var ok =
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

        ok.Click +=
            (_, _) =>
            {
                dialog.Close(
                    input.Text?.Trim());
            };

        cancel.Click +=
            (_, _) =>
            {
                dialog.Close(null);
            };

        dialog.Content =
            new StackPanel
            {
                Margin =
                    new Thickness(18),

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
                            ok
                        }
                    }
                }
            };

        return await dialog
            .ShowDialog<string?>(
                owner);
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

        var delete =
            new Button
            {
                Content = "Delete",
                MinWidth = 80
            };

        var cancel =
            new Button
            {
                Content = "Cancel",
                MinWidth = 80
            };

        delete.Click +=
            (_, _) =>
            {
                dialog.Close(true);
            };

        cancel.Click +=
            (_, _) =>
            {
                dialog.Close(false);
            };

        dialog.Content =
            new StackPanel
            {
                Margin =
                    new Thickness(18),

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
                            cancel,
                            delete
                        }
                    }
                }
            };

        return await dialog
            .ShowDialog<bool>(
                owner);
    }

    private static bool PathsEqual(
        string first,
        string second)
    {
        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        return string.Equals(
            Path.GetFullPath(first)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),

            Path.GetFullPath(second)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),

            comparison);
    }

    private static bool IsInsideDirectory(
        string file,
        string directory)
    {
        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        var filePath =
            Path.GetFullPath(file);

        var directoryPath =
            Path.GetFullPath(directory)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return filePath.StartsWith(
            directoryPath,
            comparison);
    }
}