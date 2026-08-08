namespace Axiom.Editor;

public static class LanguageService
{
    public static EditorLanguage FromFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".c" => EditorLanguage.C,

            ".h" => EditorLanguage.Cpp,
            ".hpp" => EditorLanguage.Cpp,
            ".cpp" => EditorLanguage.Cpp,
            ".cc" => EditorLanguage.Cpp,
            ".cxx" => EditorLanguage.Cpp,

            ".cs" => EditorLanguage.CSharp,

            ".rs" => EditorLanguage.Rust,

            ".py" => EditorLanguage.Python,

            ".java" => EditorLanguage.Java,

            _ => EditorLanguage.PlainText
        };
    }
}