using System.IO;

namespace Axiom.Search;

public sealed class ProjectSearchService
{
    private static readonly HashSet<string> IgnoredDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".axiom",
            "bin",
            "obj",
            "node_modules"
        };

    public async Task<List<SearchResult>> SearchAsync(
        string root,
        string query,
        CancellationToken cancellationToken = default)
    {
        var results =
            new List<SearchResult>();

        if (string.IsNullOrWhiteSpace(query))
            return results;

        foreach (var file in EnumerateFiles(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsTextFile(file))
                continue;

            string[] lines;

            try
            {
                lines =
                    await File.ReadAllLinesAsync(
                        file,
                        cancellationToken);
            }
            catch
            {
                continue;
            }

            for (var lineIndex = 0;
                 lineIndex < lines.Length;
                 lineIndex++)
            {
                var line =
                    lines[lineIndex];

                var searchStart = 0;

                while (searchStart < line.Length)
                {
                    var index =
                        line.IndexOf(
                            query,
                            searchStart,
                            StringComparison.OrdinalIgnoreCase);

                    if (index < 0)
                        break;

                    results.Add(
                        new SearchResult
                        {
                            FilePath = file,

                            RelativePath =
                                Path.GetRelativePath(
                                    root,
                                    file),

                            Line =
                                lineIndex + 1,

                            Column =
                                index + 1,

                            Preview =
                                line.Trim()
                        });

                    searchStart =
                        index + Math.Max(
                            query.Length,
                            1);
                }
            }
        }

        return results;
    }

    private static IEnumerable<string> EnumerateFiles(
        string root)
    {
        var pending =
            new Stack<string>();

        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory =
                pending.Pop();

            IEnumerable<string> directories;
            IEnumerable<string> files;

            try
            {
                directories =
                    Directory.EnumerateDirectories(
                        directory);

                files =
                    Directory.EnumerateFiles(
                        directory);
            }
            catch
            {
                continue;
            }

            foreach (var subdirectory in directories)
            {
                var name =
                    Path.GetFileName(
                        subdirectory);

                if (!IgnoredDirectories.Contains(name))
                    pending.Push(subdirectory);
            }

            foreach (var file in files)
                yield return file;
        }
    }

    private static bool IsTextFile(
        string path)
    {
        var extension =
            Path.GetExtension(path)
                .ToLowerInvariant();

        return extension is
            ".cs" or
            ".csproj" or
            ".cpp" or
            ".c" or
            ".cc" or
            ".cxx" or
            ".h" or
            ".hpp" or
            ".rs" or
            ".py" or
            ".java" or
            ".js" or
            ".ts" or
            ".json" or
            ".xml" or
            ".axaml" or
            ".xaml" or
            ".html" or
            ".css" or
            ".md" or
            ".txt" or
            ".toml" or
            ".yaml" or
            ".yml" or
            ".sh" or
            ".ps1";
    }
}