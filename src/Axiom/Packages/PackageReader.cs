using System.IO.Compression;

namespace Axiom.Packages;

public sealed class PackageReader
{
    private readonly PackageValidator _validator =
        new();

    public async Task<ImportedPackage> ImportAsync(
        string packagePath,
        string destinationRoot)
    {
        var validation =
            await _validator.ValidateAsync(
                packagePath);

        if (!validation.IsValid ||
            validation.Manifest is null)
        {
            throw new InvalidDataException(
                string.Join(
                    Environment.NewLine,
                    validation.Errors));
        }

        var manifest =
            validation.Manifest;

        var folderName =
            PackageSecurity.MakeSafeFolderName(
                manifest.Name);

        var destination =
            GetUniqueDirectory(
                destinationRoot,
                folderName);

        Directory.CreateDirectory(
            destination);

        try
        {
            using var archive =
                ZipFile.OpenRead(
                    packagePath);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(
                        entry.Name))
                {
                    continue;
                }

                if (entry.FullName.Equals(
                        "manifest.json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var output =
                    PackageSecurity
                        .GetSafeOutputPath(
                            destination,
                            entry.FullName);

                var parent =
                    Path.GetDirectoryName(
                        output);

                if (parent is not null)
                {
                    Directory.CreateDirectory(
                        parent);
                }

                await using var source =
                    entry.Open();

                await using var target =
                    new FileStream(
                        output,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None);

                await source.CopyToAsync(
                    target);
            }

            return new ImportedPackage
            {
                Manifest =
                    manifest,

                Directory =
                    destination,

                EntryPath =
                    PackageSecurity
                        .GetSafeOutputPath(
                            destination,
                            manifest.Entry)
            };
        }
        catch
        {
            try
            {
                if (Directory.Exists(
                        destination))
                {
                    Directory.Delete(
                        destination,
                        true);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    private static string GetUniqueDirectory(
        string root,
        string name)
    {
        Directory.CreateDirectory(root);

        var path =
            Path.Combine(
                root,
                name);

        if (!Directory.Exists(path))
            return path;

        for (var i = 2;
             i < 10000;
             i++)
        {
            var candidate =
                Path.Combine(
                    root,
                    $"{name} {i}");

            if (!Directory.Exists(
                    candidate))
            {
                return candidate;
            }
        }

        throw new IOException(
            "Could not create import folder.");
    }
}

public sealed class ImportedPackage
{
    public required PackageManifest Manifest
    {
        get;
        init;
    }

    public required string Directory
    {
        get;
        init;
    }

    public required string EntryPath
    {
        get;
        init;
    }
}