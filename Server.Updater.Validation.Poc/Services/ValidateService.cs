namespace Server.Updater.Validation.Poc.Services;

internal class ValidateService
{
    public ValidateService()
    {
    }

    /// <summary>
    /// Creates a manifest JSON file containing metadata for all files in the staged directory.
    /// </summary>
    /// <param name="applicationName">The application name (package directory name).</param>
    /// <returns>The path to the created manifest file.</returns>
    public string CreateManifest(string applicationName)
    {
        var stagedDirectory = UpdaterConstants.GetStagedPackageDirectory(applicationName);

        if (!Directory.Exists(stagedDirectory))
        {
            throw new DirectoryNotFoundException($"Staged directory not found: {stagedDirectory}");
        }

        var manifestEntries = new List<ManifestEntry>();
        var files = Directory.GetFiles(stagedDirectory, "*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            if (Path.GetFileName(filePath).Equals(UpdaterConstants.ManifestFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(stagedDirectory, filePath);

            manifestEntries.Add(new ManifestEntry
            {
                RelativePath = relativePath,
                SizeBytes = fileInfo.Length,
                Sha256 = FileUtilities.ComputeSha256Hash(filePath)
            });
        }

        var manifest = new PackageManifest
        {
            CreatedUtc = DateTime.UtcNow,
            FileCount = manifestEntries.Count,
            Files = manifestEntries
        };

        var manifestPath = UpdaterConstants.GetManifestPath(applicationName);
        FileUtilities.WriteJson(manifestPath, manifest);

        Console.WriteLine($"Manifest created at: {manifestPath}");
        return manifestPath;
    }

    /// <summary>
    /// Validates staged files against the manifest to ensure no tampering or corruption.
    /// Checks file count, existence, size, and SHA256 hash for each file.
    /// </summary>
    /// <param name="applicationName">The application name (package directory name).</param>
    /// <returns>The validated manifest.</returns>
    /// <exception cref="StagedArtifactValidationException">Thrown when validation fails.</exception>
    public PackageManifest ValidateStagedFiles(string applicationName)
    {
        var stagedDirectory = UpdaterConstants.GetStagedPackageDirectory(applicationName);
        var manifestPath = UpdaterConstants.GetManifestPath(applicationName);

        Console.WriteLine($"Validating staged files for: {applicationName}");

        // Load manifest
        var manifest = LoadAndValidateManifest(manifestPath);

        // Level 1: Structural validation - check file count matches
        ValidateFileCount(stagedDirectory, manifest);

        // Level 1 + Level 2: Validate each file exists, size matches, and hash matches
        foreach (var entry in manifest.Files)
        {
            var filePath = Path.Combine(stagedDirectory, entry.RelativePath);
            ValidateFile(filePath, entry);
        }

        Console.WriteLine($"All {manifest.FileCount} files validated successfully.");
        return manifest;
    }

    private static PackageManifest LoadAndValidateManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new StagedArtifactValidationException(manifestPath, ValidationFailureReason.ManifestMissing);
        }

        var manifest = FileUtilities.ReadJson<PackageManifest>(manifestPath);

        if (manifest is null || manifest.Files is null)
        {
            throw new StagedArtifactValidationException(manifestPath, ValidationFailureReason.ManifestMissing);
        }

        Console.WriteLine($"Manifest loaded: {manifest.FileCount} files, created at {manifest.CreatedUtc:u}");
        return manifest;
    }

    private static void ValidateFileCount(string stagedDirectory, PackageManifest manifest)
    {
        var actualFiles = Directory.GetFiles(stagedDirectory, "*", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Equals(UpdaterConstants.ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (actualFiles.Count != manifest.FileCount)
        {
            throw new StagedArtifactValidationException(
                stagedDirectory,
                ValidationFailureReason.FileCountMismatch);
        }
    }

    private static void ValidateFile(string filePath, ManifestEntry entry)
    {
        // Check file exists
        if (!File.Exists(filePath))
        {
            throw new StagedArtifactValidationException(filePath, ValidationFailureReason.FileMissing);
        }

        var fileInfo = new FileInfo(filePath);

        // Check size matches (Level 1 - structural)
        if (fileInfo.Length != entry.SizeBytes)
        {
            throw new StagedArtifactValidationException(filePath, ValidationFailureReason.SizeMismatch);
        }

        // Check hash matches (Level 2 - content integrity)
        var actualHash = FileUtilities.ComputeSha256Hash(filePath);
        if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new StagedArtifactValidationException(filePath, ValidationFailureReason.HashMismatch);
        }
    }
}

/// <summary>
/// Represents the complete package manifest.
/// </summary>
internal class PackageManifest
{
    public DateTime CreatedUtc { get; set; }
    public int FileCount { get; set; }
    public List<ManifestEntry> Files { get; set; } = [];
}

/// <summary>
/// Represents a single file entry in the manifest.
/// </summary>
internal class ManifestEntry
{
    public required string RelativePath { get; set; }
    public long SizeBytes { get; set; }
    public required string Sha256 { get; set; }
}
