using Server.Updater.Validation.Poc.Services;

// Configuration
const string nupkgFilePath = @"C:\tmp\poc\AppServer-64.25.1.17.1000.nupkg";
var applicationName = Path.GetFileNameWithoutExtension(nupkgFilePath);

try
{
    // Stage the package (extracts .nupkg and creates manifest)
    Console.WriteLine("=== STAGING PHASE ===");
    var stagingService = new StagingService(nupkgFilePath);
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

