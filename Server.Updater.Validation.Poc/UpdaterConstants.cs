namespace Server.Updater.Validation.Poc;

/// <summary>
/// Central configuration constants for update service paths and settings.
/// </summary>
internal static class UpdaterConstants
{
    /// <summary>
    /// The application name to test. Change this to test different nupkg files.
    /// </summary>
    public const string ApplicationName = "AppServer-64.25.1.17.1000";

    /// <summary>
    /// Root directory for all updater operations.
    /// </summary>
    public const string RootDirectory = @"C:\tmp\poc";

    /// <summary>
    /// Directory where .nupkg files are cached before staging.
    /// </summary>
    public static string CacheDirectory => Path.Combine(RootDirectory, "cache");

    /// <summary>
    /// Directory where packages are staged (extracted) before installation.
    /// </summary>
    public static string StagingDirectory => Path.Combine(RootDirectory, "staging");

    /// <summary>
    /// Directory where applications are installed.
    /// </summary>
    public static string InstallDirectory => Path.Combine(RootDirectory, "install");

    /// <summary>
    /// Name of the manifest file created during staging.
    /// </summary>
    public const string ManifestFileName = "manifest.json";

    /// <summary>
    /// Extension for NuGet package files.
    /// </summary>
    public const string NupkgExtension = ".nupkg";

    /// <summary>
    /// Gets the nupkg file path for a specific application from the cache directory.
    /// </summary>
    public static string GetNupkgFilePath(string applicationName)
        => Path.Combine(CacheDirectory, applicationName + NupkgExtension);

    /// <summary>
    /// Gets the staged package directory for a specific application.
    /// </summary>
    public static string GetStagedPackageDirectory(string applicationName)
        => Path.Combine(StagingDirectory, applicationName);

    /// <summary>
    /// Gets the install target directory for a specific application.
    /// </summary>
    public static string GetInstallDirectory(string applicationName)
        => Path.Combine(InstallDirectory, applicationName);

    /// <summary>
    /// Gets the manifest file path for a staged package.
    /// </summary>
    public static string GetManifestPath(string applicationName)
        => Path.Combine(GetStagedPackageDirectory(applicationName), ManifestFileName);
}
