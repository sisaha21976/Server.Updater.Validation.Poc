namespace Server.Updater.Validation.Poc.Services;

using System.IO.Compression;

internal class StagingService
{
    private static readonly HashSet<string> _foldersToIgnore = ["_rels"];
    private static readonly HashSet<string> _filesToIgnore = ["[Content_Types].xml", ".signature.p7s"];
    private static readonly HashSet<string> _fileExtensionsToIgnore = [".nuspec"];

    private readonly string _applicationName;
    private readonly string _nupkgFilePath;

    public StagingService(string applicationName)
    {
        _applicationName = applicationName;
        _nupkgFilePath = UpdaterConstants.GetNupkgFilePath(applicationName);
    }

    public void Run()
    {
        ValidatePaths();
        ExtractPackage();

        var validateService = new ValidateService();
        validateService.CreateManifest(_applicationName);

        Console.WriteLine($"Package staged successfully at: {UpdaterConstants.GetStagedPackageDirectory(_applicationName)}");
    }

    private void ValidatePaths()
    {
        if (!File.Exists(_nupkgFilePath))
        {
            throw new FileNotFoundException($"Nupkg file not found: {_nupkgFilePath}");
        }

        FileUtilities.EnsureDirectoryExists(UpdaterConstants.StagingDirectory);
    }

    private void ExtractPackage()
    {
        string destinationDirectory = UpdaterConstants.GetStagedPackageDirectory(_applicationName);
        FileUtilities.CleanDirectory(destinationDirectory);

        using Stream stream = File.Open(_nupkgFilePath, FileMode.Open, FileAccess.Read);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            string fullPath = FileUtilities.GetSafeFullPath(entry.FullName, destinationDirectory);

            if (!_foldersToIgnore.Contains(Directory.GetParent(fullPath)?.Name!) &&
                !_filesToIgnore.Contains(entry.Name) &&
                !_fileExtensionsToIgnore.Contains(Path.GetExtension(entry.Name)))
            {
                string? directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    FileUtilities.EnsureDirectoryExists(directoryPath);
                }

                using Stream sourceStream = entry.Open();
                using Stream destinationStream = File.Create(fullPath);
                sourceStream.CopyTo(destinationStream);
            }
        }
    }
}