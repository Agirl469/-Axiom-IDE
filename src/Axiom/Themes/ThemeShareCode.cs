using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Axiom.Themes;

public static class ThemeShareCode
{
    private const string Prefix = "axiom-theme://AXT1.";

    public static string Encode(AxiomTheme theme)
    {
        var json = ThemeSerializer.Serialize(theme);
        var raw = Encoding.UTF8.GetBytes(json);

        byte[] compressed;

        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(
                       output,
                       CompressionLevel.SmallestSize,
                       leaveOpen: true))
            {
                gzip.Write(raw, 0, raw.Length);
            }

            compressed = output.ToArray();
        }

        var checksum = SHA256.HashData(compressed);

        var payload = new byte[4 + compressed.Length];

        Buffer.BlockCopy(
            checksum,
            0,
            payload,
            0,
            4);

        Buffer.BlockCopy(
            compressed,
            0,
            payload,
            4,
            compressed.Length);

        return Prefix + ToBase64Url(payload);
    }

    public static AxiomTheme Decode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Theme code is empty.");

        code = code.Trim();

        if (!code.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "This is not a valid Axiom theme code.");

        var encoded = code[Prefix.Length..];
        var payload = FromBase64Url(encoded);

        if (payload.Length < 5)
            throw new InvalidOperationException(
                "Theme code is incomplete.");

        var storedChecksum = payload[..4];
        var compressed = payload[4..];

        var checksum = SHA256.HashData(compressed);

        if (!storedChecksum.SequenceEqual(checksum[..4]))
            throw new InvalidOperationException(
                "Theme code is damaged or invalid.");

        using var input = new MemoryStream(compressed);

        using var gzip = new GZipStream(
            input,
            CompressionMode.Decompress);

        using var output = new MemoryStream();

        gzip.CopyTo(output);

        var json =
            Encoding.UTF8.GetString(
                output.ToArray());

        return ThemeSerializer.Deserialize(json);
    }

    private static string ToBase64Url(byte[] data)
    {
        return Convert
            .ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = base64.Length % 4;

        if (padding == 2)
            base64 += "==";
        else if (padding == 3)
            base64 += "=";
        else if (padding == 1)
            throw new InvalidOperationException(
                "Theme code has invalid encoding.");

        return Convert.FromBase64String(base64);
    }
}