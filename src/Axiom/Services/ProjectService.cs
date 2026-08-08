using System.Text.Json;
using Axiom.Models;

namespace Axiom.Services;

public sealed class ProjectService
{
    public const string ProjectExtension = ".axn";

    public string? FindProjectFile(string root)
    {
        if (!Directory.Exists(root))
            return null;

        return Directory.EnumerateFiles(root, $"*{ProjectExtension}", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public async Task<AxiomProject?> LoadAsync(string root)
    {
        var path = FindProjectFile(root);
        return path is null ? null : await LoadFileAsync(path);
    }

    public async Task<AxiomProject?> LoadFileAsync(string path)
    {
        if (!File.Exists(path) || !Path.GetExtension(path).Equals(ProjectExtension, StringComparison.OrdinalIgnoreCase))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<AxiomProject>(json, JsonOptions());
    }

    public async Task<string> SaveAsync(string root, AxiomProject project)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, MakeProjectFileName(project.Name));
        var json = JsonSerializer.Serialize(project, JsonOptions());
        await File.WriteAllTextAsync(path, json + Environment.NewLine);
        return path;
    }

    public IEnumerable<string> GetSourceFiles(string root)
    {
        if (!Directory.Exists(root))
            return [];

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".axn", ".cs", ".cpp", ".c", ".h", ".hpp", ".rs", ".java", ".py",
            ".js", ".ts", ".json", ".xml", ".xaml", ".axaml", ".md", ".toml",
            ".yaml", ".yml", ".sh", ".ps1", ".txt"
        };

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsIgnored(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    public string Describe(AxiomProject? project)
    {
        if (project is null)
            return "Folder";

        return project.Language switch
        {
            "cpp" => "Axiom C++ project",
            "csharp" => "Axiom C# project",
            "rust" => "Axiom Rust project",
            "python" => "Axiom Python project",
            _ => "Axiom project"
        };
    }

    private static string MakeProjectFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return (string.IsNullOrWhiteSpace(cleaned) ? "project" : cleaned) + ProjectExtension;
    }

    private static bool IsIgnored(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}.git{separator}") ||
               path.Contains($"{separator}.axiom{separator}") ||
               path.Contains($"{separator}bin{separator}") ||
               path.Contains($"{separator}obj{separator}");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
