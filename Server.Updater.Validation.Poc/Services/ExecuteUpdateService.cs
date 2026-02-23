namespace Server.Updater.Validation.Poc.Services;

/// <summary>
/// Service responsible for executing the update by validating staged files against the manifest
/// and copying them to the installation directory.
/// </summary>
internal class ExecuteUpdateService
{
    private readonly string _applicationName;
    private readonly string _stagedPackageDirectory;
    private readonly string _installTargetDirectory;
    private readonly ValidateService _validateService;
    private readonly MetricsService _metricsService;

    public ExecuteUpdateService(string applicationName)
    {
        _applicationName = applicationName;
        _stagedPackageDirectory = UpdaterConstants.GetStagedPackageDirectory(applicationName);
        _installTargetDirectory = UpdaterConstants.GetInstallDirectory(applicationName);
        _validateService = new ValidateService();
        _metricsService = new MetricsService();
    }

    /// <summary>
    /// Executes the update by validating each staged file and copying it immediately.
    /// Single pass: validate -> copy for each file (fail fast on validation failure).
    /// </summary>
    public void Execute()
    {
        _metricsService.StartTotal();

        Console.WriteLine($"Starting update execution for: {_applicationName}");

        var fileCount = GetFileCount();
        var totalBytes = GetTotalBytes();

        // Single pass: Validate each file then copy it immediately
        _metricsService.MeasureWithFileCount(
            "Validate + Copy",
            fileCount,
            totalBytes,
            () => ValidateAndCopyFiles());

        Console.WriteLine($"Update executed successfully. Files installed at: {_installTargetDirectory}");

        _metricsService.PrintMetrics();
    }

    /// <summary>
    /// Executes the update with separate validation and copy phases (for comparison).
    /// This is where validation is done for all files first, and if successful, proceeds to copy all files.
    /// This allows us to compare the performance and failure modes of separate phases vs. single pass.
    /// </summary>
    [Obsolete("This method is for comparison purposes only. Use Execute() for the recommended single pass approach.")]
    public void ExecuteSeparatePhases()
    {
        _metricsService.StartTotal();

        Console.WriteLine($"Starting update execution for: {_applicationName}");

        // Validate staged files against manifest (size + hash check)
        var manifest = _metricsService.MeasureWithFileCount(
            "Validation",
            GetFileCount(),
            GetTotalBytes(),
            () => _validateService.ValidateStagedFiles(_applicationName));

        // Copy validated files to install directory
        _metricsService.MeasureWithFileCount(
            "File Copy",
            manifest.FileCount,
            manifest.Files.Sum(f => f.SizeBytes),
            () => CopyFilesToInstallDirectory(manifest));

        Console.WriteLine($"Update executed successfully. Files installed at: {_installTargetDirectory}");

        _metricsService.PrintMetrics();
    }

    private void ValidateAndCopyFiles()
    {
        Console.WriteLine($"Validating and copying staged files for: {_applicationName}");

        // Load manifest and validate file count (structural validation)
        var manifest = _validateService.LoadAndValidateStructure(_applicationName);

        // Prepare install directory
        FileUtilities.CleanDirectory(_installTargetDirectory);

        // Single pass: Validate each file then immediately copy it
        foreach (var entry in manifest.Files)
        {
            var sourcePath = Path.Combine(_stagedPackageDirectory, entry.RelativePath);
            var destinationPath = Path.Combine(_installTargetDirectory, entry.RelativePath);

            // Validate file (size + hash) - throws on failure
            _validateService.ValidateFile(sourcePath, entry);

            // Copy immediately after successful validation
            FileUtilities.CopyFileWithDirectory(sourcePath, destinationPath);
        }

        Console.WriteLine($"All {manifest.FileCount} files validated and copied successfully.");
    }

    private int GetFileCount()
    {
        return Directory.GetFiles(_stagedPackageDirectory, "*", SearchOption.AllDirectories)
            .Count(f => !Path.GetFileName(f).Equals(UpdaterConstants.ManifestFileName, StringComparison.OrdinalIgnoreCase));
    }

    private long GetTotalBytes()
    {
        return Directory.GetFiles(_stagedPackageDirectory, "*", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Equals(UpdaterConstants.ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .Sum(f => new FileInfo(f).Length);
    }

    private void CopyFilesToInstallDirectory(PackageManifest manifest)
    {
        Console.WriteLine($"Copying files to install directory: {_installTargetDirectory}");

        FileUtilities.CleanDirectory(_installTargetDirectory);

        foreach (var entry in manifest.Files)
        {
            var sourcePath = Path.Combine(_stagedPackageDirectory, entry.RelativePath);
            var destinationPath = Path.Combine(_installTargetDirectory, entry.RelativePath);
            FileUtilities.CopyFileWithDirectory(sourcePath, destinationPath);
        }

        Console.WriteLine($"Copied {manifest.FileCount} files to install directory.");
    }
}
