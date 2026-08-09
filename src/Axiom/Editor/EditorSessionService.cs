using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Axiom.Editor;

public sealed class EditorSessionService
{
    private readonly string _root;
    private readonly string _sessionFile;

    public EditorSessionService(string root)
    {
        _root = Path.GetFullPath(root);

        var appData = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "Axiom",
            "Sessions");

        Directory.CreateDirectory(appData);

        _sessionFile = Path.Combine(
            appData,
            CreateProjectId(_root) + ".json");
    }

    public async Task SaveAsync(EditorSession session)
    {
        var json =
            JsonSerializer.Serialize(
                session,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            _sessionFile,
            json);
    }

    public async Task<EditorSession?> LoadAsync()
    {
        if (!File.Exists(_sessionFile))
            return null;

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    _sessionFile);

            return JsonSerializer.Deserialize<EditorSession>(
                json);
        }
        catch
        {
            return null;
        }
    }

    public string ToStoredPath(string file)
    {
        return Path.GetRelativePath(
                _root,
                file)
            .Replace('\\', '/');
    }

    public string ToFullPath(string storedPath)
    {
        var normalized =
            storedPath.Replace(
                '/',
                Path.DirectorySeparatorChar);

        return Path.GetFullPath(
            Path.Combine(
                _root,
                normalized));
    }

    private static string CreateProjectId(string root)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    root.ToLowerInvariant()));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant()[..20];
    }
}

public sealed class EditorSession
{
    public List<EditorSessionTab> Tabs { get; set; } = [];

    public string? ActiveFile { get; set; }
}

public sealed class EditorSessionTab
{
    public string File { get; set; } =
        string.Empty;

    public int CaretIndex { get; set; }
}