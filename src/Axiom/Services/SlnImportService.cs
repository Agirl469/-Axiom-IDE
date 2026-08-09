using System.Text.RegularExpressions;
using System.Xml.Linq;
using Axiom.Models;

namespace Axiom.Services;

public sealed class SlnImportService
{
    private readonly ProjectService _projects = new();

    private static readonly Regex ProjectLine =
        new(
            @"Project\("".*?""\)\s*=\s*"".*?"",\s*""(?<path>.*?)"",",
            RegexOptions.Compiled);

    public async Task<List<SlnProjectInfo>> ReadProjectsAsync(
        string solutionPath)
    {
        var root =
            Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException(
                "Could not find the solution directory.");

        var text =
            await File.ReadAllTextAsync(solutionPath);

        var projects =
            new List<SlnProjectInfo>();

        foreach (Match match in ProjectLine.Matches(text))
        {
            var relativePath =
                match.Groups["path"]
                    .Value
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar);

            var fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        root,
                        relativePath));

            var extension =
                Path.GetExtension(fullPath)
                    .ToLowerInvariant();

            if (extension is not ".csproj" and not ".vcxproj")
                continue;

            projects.Add(
                new SlnProjectInfo
                {
                    ProjectFile = fullPath,
                    Name =
                        Path.GetFileNameWithoutExtension(
                            fullPath),

                    Type =
                        extension == ".csproj"
                            ? "C#"
                            : "C++"
                });
        }

        return projects;
    }

    public async Task<string> ImportAsync(
        SlnProjectInfo source)
    {
        if (!File.Exists(source.ProjectFile))
        {
            throw new FileNotFoundException(
                "The Visual Studio project could not be found.",
                source.ProjectFile);
        }

        var root =
            Path.GetDirectoryName(
                source.ProjectFile)
            ?? throw new InvalidOperationException(
                "Could not determine the project folder.");

        var extension =
            Path.GetExtension(
                    source.ProjectFile)
                .ToLowerInvariant();

        var project =
            extension switch
            {
                ".csproj" =>
                    await ImportCSharpAsync(
                        source.ProjectFile,
                        root),

                ".vcxproj" =>
                    await ImportCppAsync(
                        source.ProjectFile,
                        root),

                _ =>
                    throw new NotSupportedException(
                        $"Axiom cannot import {extension} projects yet.")
            };

        await _projects.SaveAsync(
            root,
            project);

        return root;
    }

    private static async Task<AxiomProject> ImportCSharpAsync(
        string projectFile,
        string root)
    {
        var document =
            XDocument.Load(projectFile);

        var targetFramework =
            document
                .Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName ==
                    "TargetFramework")
                ?.Value
            ?? "net10.0";

        var entry =
            FindFile(
                root,
                "Program.cs")
            ?? FindFirstFile(
                root,
                "*.cs");

        await Task.CompletedTask;

        return new AxiomProject
        {
            Name =
                Path.GetFileNameWithoutExtension(
                    projectFile),

            Language = "csharp",

            Entry =
                entry is null
                    ? null
                    : Path.GetRelativePath(
                            root,
                            entry)
                        .Replace('\\', '/'),

            Settings =
                new Dictionary<string, string>
                {
                    ["targetFramework"] =
                        targetFramework,

                    ["importedFrom"] =
                        Path.GetFileName(
                            projectFile)
                }
        };
    }

    private static async Task<AxiomProject> ImportCppAsync(
        string projectFile,
        string root)
    {
        var entry =
            FindFile(
                root,
                "main.cpp")
            ?? FindFirstFile(
                root,
                "*.cpp");

        await Task.CompletedTask;

        return new AxiomProject
        {
            Name =
                Path.GetFileNameWithoutExtension(
                    projectFile),

            Language = "cpp",

            Entry =
                entry is null
                    ? null
                    : Path.GetRelativePath(
                            root,
                            entry)
                        .Replace('\\', '/'),

            Settings =
                new Dictionary<string, string>
                {
                    ["compiler"] =
                        "g++",

                    ["standard"] =
                        "c++20",

                    ["importedFrom"] =
                        Path.GetFileName(
                            projectFile)
                }
        };
    }

    private static string? FindFile(
        string root,
        string name)
    {
        try
        {
            return Directory
                .EnumerateFiles(
                    root,
                    name,
                    SearchOption.AllDirectories)
                .FirstOrDefault(
                    path =>
                        !Ignored(path));
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFirstFile(
        string root,
        string pattern)
    {
        try
        {
            return Directory
                .EnumerateFiles(
                    root,
                    pattern,
                    SearchOption.AllDirectories)
                .FirstOrDefault(
                    path =>
                        !Ignored(path));
        }
        catch
        {
            return null;
        }
    }

    private static bool Ignored(
        string path)
    {
        var parts =
            path.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        return parts.Any(part =>
            part.Equals(
                "bin",
                StringComparison.OrdinalIgnoreCase) ||
            part.Equals(
                "obj",
                StringComparison.OrdinalIgnoreCase) ||
            part.Equals(
                ".git",
                StringComparison.OrdinalIgnoreCase) ||
            part.Equals(
                ".axiom",
                StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class SlnProjectInfo
{
    public string Name { get; set; } =
        string.Empty;

    public string Type { get; set; } =
        string.Empty;

    public string ProjectFile { get; set; } =
        string.Empty;

    public override string ToString()
    {
        return $"{Name} ({Type})";
    }
}