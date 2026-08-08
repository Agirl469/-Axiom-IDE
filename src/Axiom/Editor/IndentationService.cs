namespace Axiom.Editor;

public static class IndentationService
{
    public const int TabSize = 4;

    public static string GetIndentForNewLine(
        EditorLanguage language,
        string currentLine)
    {
        var indent = GetLeadingWhitespace(currentLine);
        var trimmed = currentLine.TrimEnd();

        if (ShouldIncreaseIndent(language, trimmed))
            indent += new string(' ', TabSize);

        return indent;
    }

    private static bool ShouldIncreaseIndent(
        EditorLanguage language,
        string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        return language switch
        {
            EditorLanguage.C or
            EditorLanguage.Cpp or
            EditorLanguage.CSharp or
            EditorLanguage.Rust or
            EditorLanguage.Java
                => line.EndsWith("{"),

            EditorLanguage.Python
                => line.EndsWith(":"),

            _ => false
        };
    }

    private static string GetLeadingWhitespace(string line)
    {
        var length = 0;

        while (length < line.Length)
        {
            var c = line[length];

            if (c != ' ' && c != '\t')
                break;

            length++;
        }

        return line[..length];
    }
}