using System.IO.Compression;
using System.Text.Json;

namespace Axiom.Packages;

public sealed class PackageValidator
{
    public async Task<PackageValidationResult> ValidateAsync(
        string packagePath)
    {
        var result =
            new PackageValidationResult();

        if (!File.Exists(packagePath))
        {
            result.Errors.Add(
                "Package does not exist.");

            return result;
        }

        try
        {
            using var archive =
                ZipFile.OpenRead(
                    packagePath);

            if (archive.Entries.Count >
                PackageSecurity.MaxFiles)
            {
                result.Errors.Add(
                    $"Package contains too many files. Maximum is {PackageSecurity.MaxFiles}.");

                return result;
            }

            var manifestEntry =
                archive.GetEntry(
                    "manifest.json");

            if (manifestEntry is null)
            {
                result.Errors.Add(
                    "manifest.json is missing.");

                return result;
            }

            PackageManifest? manifest;

            await using (
                var stream =
                    manifestEntry.Open())
            {
                manifest =
                    await JsonSerializer
                        .DeserializeAsync<PackageManifest>(
                            stream);
            }

            if (manifest is null)
            {
                result.Errors.Add(
                    "Manifest could not be read.");

                return result;
            }

            result.Manifest =
                manifest;

            ValidateManifest(
                manifest,
                result);

            if (!result.IsValid)
                return result;

            long totalSize = 0;

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(
                        entry.Name))
                {
                    continue;
                }

                if (!PackageSecurity
                    .IsSafeArchivePath(
                        entry.FullName))
                {
                    result.Errors.Add(
                        $"Unsafe path: {entry.FullName}");

                    continue;
                }

                if (entry.Length >
                    PackageSecurity.MaxSingleFileSize)
                {
                    result.Errors.Add(
                        $"{entry.FullName} is too large.");

                    continue;
                }

                totalSize +=
                    entry.Length;

                if (totalSize >
                    PackageSecurity.MaxTotalSize)
                {
                    result.Errors.Add(
                        "Package expands beyond the allowed total size.");

                    break;
                }

                if (entry.FullName.Equals(
                        "manifest.json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!PackageSecurity.IsAllowedFile(
                        manifest.Type,
                        entry.FullName))
                {
                    result.Errors.Add(
                        $"File type is not allowed: {entry.FullName}");
                }
            }

            if (!result.IsValid)
                return result;

            await ValidateHashesAsync(
                archive,
                manifest,
                result);

            return result;
        }
        catch (InvalidDataException ex)
        {
            result.Errors.Add(
                $"Invalid package: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.Errors.Add(
                $"Package could not be checked: {ex.Message}");
        }

        return result;
    }

    private static void ValidateManifest(
        PackageManifest manifest,
        PackageValidationResult result)
    {
        if (manifest.Format != 1)
        {
            result.Errors.Add(
                $"Unsupported package format: {manifest.Format}");
        }

        if (manifest.Type is not
            "effect" and not
            "theme")
        {
            result.Errors.Add(
                $"Unsupported package type: {manifest.Type}");
        }

        if (string.IsNullOrWhiteSpace(
                manifest.Name))
        {
            result.Errors.Add(
                "Package name is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                manifest.Entry))
        {
            result.Errors.Add(
                "Package entry file is missing.");
        }

        if (!PackageSecurity
            .IsSafeArchivePath(
                manifest.Entry))
        {
            result.Errors.Add(
                "Manifest entry path is unsafe.");
        }

        if (manifest.Files.Count >
            PackageSecurity.MaxFiles)
        {
            result.Errors.Add(
                "Manifest contains too many files.");
        }
    }

    private static async Task ValidateHashesAsync(
        ZipArchive archive,
        PackageManifest manifest,
        PackageValidationResult result)
    {
        foreach (var expected in manifest.Files)
        {
            if (!PackageSecurity
                .IsSafeArchivePath(
                    expected.Path))
            {
                result.Errors.Add(
                    $"Unsafe manifest path: {expected.Path}");

                continue;
            }

            var entry =
                archive.GetEntry(
                    expected.Path);

            if (entry is null)
            {
                result.Errors.Add(
                    $"Missing package file: {expected.Path}");

                continue;
            }

            if (entry.Length !=
                expected.Size)
            {
                result.Errors.Add(
                    $"Size mismatch: {expected.Path}");

                continue;
            }

            await using var stream =
                entry.Open();

            var actual =
                await PackageHash
                    .ComputeAsync(
                        stream);

            if (!actual.Equals(
                    expected.Sha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(
                    $"Checksum mismatch: {expected.Path}");
            }
        }
    }
}