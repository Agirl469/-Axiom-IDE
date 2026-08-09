namespace Axiom.Search;

public sealed class SearchResult
{
    public string FilePath { get; set; } =
        string.Empty;

    public string RelativePath { get; set; } =
        string.Empty;

    public int Line { get; set; }

    public int Column { get; set; }

    public string Preview { get; set; } =
        string.Empty;

    public override string ToString()
    {
        return $"{RelativePath}:{Line}:{Column}    {Preview}";
    }
}