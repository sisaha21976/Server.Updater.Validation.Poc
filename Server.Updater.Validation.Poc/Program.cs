using Server.Updater.Validation.Poc;
using Server.Updater.Validation.Poc.Services;

// Application name is configured in UpdaterConstants.ApplicationName
// The nupkg file is expected at: {CacheDirectory}/{ApplicationName}.nupkg
var applicationName = UpdaterConstants.ApplicationName;

Console.WriteLine($"Application: {applicationName}");
Console.WriteLine($"Nupkg path:  {UpdaterConstants.GetNupkgFilePath(applicationName)}");
Console.WriteLine($"Stage path:  {UpdaterConstants.GetStagedPackageDirectory(applicationName)}");
Console.WriteLine($"Install path: {UpdaterConstants.GetInstallDirectory(applicationName)}");
Console.WriteLine();

try
{
    // Stage the package (extracts .nupkg and creates manifest)
    Console.WriteLine("=== STAGING PHASE ===");
    var stagingService = new StagingService(applicationName);
    stagingService.Run();

    // Execute the update (validates staged files and copies to install directory)
    Console.WriteLine();
    Console.WriteLine("=== EXECUTE PHASE ===");
    var executeService = new ExecuteUpdateService(applicationName);
    executeService.Execute();
}
catch (StagedArtifactValidationException ex)
{
    Console.WriteLine();
    Console.WriteLine($"Validation failed: {ex.Message}");
    Console.WriteLine($"  File: {ex.FilePath}");
    Console.WriteLine($"  Reason: {ex.Reason}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

