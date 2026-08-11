using System.Security.Cryptography;

namespace Axiom.Packages;

public static class PackageHash
{
    public static async Task<string> ComputeAsync(
        Stream stream)
    {
        using var sha =
            SHA256.Create();

        var hash =
            await sha.ComputeHashAsync(
                stream);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }

    public static async Task<string> ComputeFileAsync(
        string path)
    {
        await using var stream =
            File.OpenRead(path);

        return await ComputeAsync(stream);
    }
}