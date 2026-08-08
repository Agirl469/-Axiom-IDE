namespace Axiom.Editor;

public sealed class EditorTab
{
    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public required EditorLanguage Language { get; init; }

    public string Text { get; set; } = string.Empty;

    public bool IsDirty { get; set; }

    public int CaretIndex { get; set; }

    public string DisplayName =>
        IsDirty
            ? $"{FileName} *"
            : FileName;
}