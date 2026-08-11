using System.IO.Compression;
using System.Text.Json;

namespace Axiom.Packages;

public sealed class PackageWriter
{
    public async Task WriteAsync(
        string sourceDirectory,
        string destination,
        PackageManifest manifest)
    {
        if (!Directory.Exists(
                sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                sourceDirectory);
        }

        var files =
            Directory
                .EnumerateFiles(
                    sourceDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(
                    file =>
                        !Path.GetFileName(file)
                            .Equals(
                                "manifest.json",
                                StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (files.Count >
            PackageSecurity.MaxFiles)
        {
            throw new InvalidOperationException(
                "Effect/theme contains too many files.");
        }

        manifest.Files.Clear();

        long totalSize = 0;

        foreach (var file in files)
        {
            var relative =
                Path.GetRelativePath(
                        sourceDirectory,
                        file)
                    .Replace(
                        '\\',
                        '/');

            if (!PackageSecurity.IsAllowedFile(
                    manifest.Type,
                    relative))
            {
                throw new InvalidOperationException(
                    $"File type is not allowed: {relative}");
            }

            var info =
                new FileInfo(file);

            if (info.Length >
                PackageSecurity.MaxSingleFileSize)
            {
                throw new InvalidOperationException(
                    $"{relative} is too large.");
            }

            totalSize +=
                info.Length;

            if (totalSize >
                PackageSecurity.MaxTotalSize)
            {
                throw new InvalidOperationException(
                    "Package exceeds the maximum size.");
            }

            manifest.Files.Add(
                new PackageFile
                {
                    Path =
                        relative,

                    Size =
                        info.Length,

                    Sha256 =
                        await PackageHash
                            .ComputeFileAsync(
                                file)
                });
        }

        var temp =
            destination +
            ".tmp";

        if (File.Exists(temp))
            File.Delete(temp);

        using (
            var archive =
                ZipFile.Open(
                    temp,
                    ZipArchiveMode.Create))
        {
            foreach (var file in files)
            {
                var relative =
                    Path.GetRelativePath(
                            sourceDirectory,
                            file)
                        .Replace(
                            '\\',
                            '/');

                archive.CreateEntryFromFile(
                    file,
                    relative,
                    CompressionLevel.Optimal);
            }

            var manifestEntry =
                archive.CreateEntry(
                    "manifest.json",
                    CompressionLevel.Optimal);

            await using var stream =
                manifestEntry.Open();

            await JsonSerializer.SerializeAsync(
                stream,
                manifest,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(
            temp,
            destination);
    }
}