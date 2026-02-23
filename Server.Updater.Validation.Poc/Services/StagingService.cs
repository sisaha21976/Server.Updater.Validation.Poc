namespace Server.Updater.Validation.Poc.Services;

using System.IO.Compression;

internal class StagingService
{
    // Same filters as PackageRepository
    private static readonly HashSet<string> _foldersToIgnore = ["_rels"];
    private static readonly HashSet<string> _filesToIgnore = ["[Content_Types].xml", ".signature.p7s"];
    private static readonly HashSet<string> _fileExtensionsToIgnore = [".nuspec"];

    private const string NupkgFilePath = @"C:\tmp\poc\AppServer-64.25.1.17.1000.nupkg";
    private const string StagingDirectory = @"C:\tmp\poc\staging";

    public StagingService()
    {
        Run();
    }

    private void Run()
    {
        ValidatePaths();
        InstallPackage();
        Console.WriteLine($"Package staged successfully at: {GetDestinationDirectory()}");
    }

    private void ValidatePaths()
    {
        if (!File.Exists(NupkgFilePath))
        {
            throw new FileNotFoundException($"Nupkg file not found: {NupkgFilePath}");
        }

        if (!Directory.Exists(StagingDirectory))
        {
            Directory.CreateDirectory(StagingDirectory);
        }
    }

    private string GetDestinationDirectory()
    {
        return Path.Combine(StagingDirectory, Path.GetFileNameWithoutExtension(NupkgFilePath));
    }

    private void InstallPackage()
    {
        string destinationDirectory = GetDestinationDirectory();

        // Clean up existing staged package
        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(destinationDirectory, recursive: true);
        }

        // Open nupkg as ZipArchive (same as PackageRepository)
        using Stream stream = File.Open(NupkgFilePath, FileMode.Open, FileAccess.Read);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // Skip directory entries
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            string fullPath = GetFullPath(entry.FullName, destinationDirectory);

            // Apply same filters as PackageRepository
            if (!_foldersToIgnore.Contains(Directory.GetParent(fullPath)?.Name!) &&
                !_filesToIgnore.Contains(entry.Name) &&
                !_fileExtensionsToIgnore.Contains(Path.GetExtension(entry.Name)))
            {
                // Create directory structure
                string? directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Extract file
                using Stream sourceStream = entry.Open();
                using Stream destinationStream = File.Create(fullPath);
                sourceStream.CopyTo(destinationStream);
            }
        }
    }

    // Same as ZipUtilities.GetFullPath
    private static string GetFullPath(string entryFullName, string rootPath)
    {
        string fileName = Path.Combine(rootPath, entryFullName);
        string fullPath = Path.GetFullPath(fileName);

        // Zip slip vulnerability check
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Attempt to extract file outside target directory: {fullPath}");
        }

        return fullPath;
    }
}