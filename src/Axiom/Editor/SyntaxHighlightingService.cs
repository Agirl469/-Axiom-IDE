using AvaloniaEdit.Highlighting;

namespace Axiom.Editor;

public static class SyntaxHighlightingService
{
    public static IHighlightingDefinition? GetForFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var extension = Path.GetExtension(path).ToLowerInvariant();

        var definitionName = extension switch
        {
            ".cs" => "C#",

            ".c" or
            ".cc" or
            ".cpp" or
            ".cxx" or
            ".h" or
            ".hh" or
            ".hpp" or
            ".hxx" => "C++",

            ".xml" or
            ".xaml" or
            ".axaml" => "XML",

            ".html" or
            ".htm" => "HTML",

            ".js" or
            ".mjs" or
            ".cjs" or
            ".json" => "JavaScript",

            ".py" => "Python",
            ".java" => "Java",
            ".php" => "PHP",
            ".sql" => "SQL",

            _ => null
        };

        if (definitionName is null)
            return null;

        return HighlightingManager.Instance.GetDefinition(definitionName);
    }
}