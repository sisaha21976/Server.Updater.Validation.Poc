namespace Server.Updater.Validation.Poc.Services;

/// <summary>
/// Exception thrown when staged artifact validation fails during execute phase.
/// </summary>
internal class StagedArtifactValidationException : Exception
{
    public string FilePath { get; }
    public ValidationFailureReason Reason { get; }

    public StagedArtifactValidationException(string filePath, ValidationFailureReason reason)
        : base(GetMessage(filePath, reason))
    {
        FilePath = filePath;
        Reason = reason;
    }

    public StagedArtifactValidationException(string filePath, ValidationFailureReason reason, Exception innerException)
        : base(GetMessage(filePath, reason), innerException)
    {
        FilePath = filePath;
        Reason = reason;
    }

    private static string GetMessage(string filePath, ValidationFailureReason reason)
    {
        return reason switch
        {
            ValidationFailureReason.ManifestMissing => "Manifest file not found in staged directory. Package may not be staged correctly.",
            ValidationFailureReason.FileMissing => $"Staged file not found: {filePath}",
            ValidationFailureReason.SizeMismatch => $"File size mismatch for: {filePath}",
            ValidationFailureReason.HashMismatch => $"File hash mismatch for: {filePath}",
            ValidationFailureReason.FileCountMismatch => "File count in staged directory does not match manifest.",
            _ => $"Validation failed for: {filePath}"
        };
    }
}

/// <summary>
/// Reasons for staged artifact validation failure.
/// </summary>
internal enum ValidationFailureReason
{
    ManifestMissing,
    FileMissing,
    SizeMismatch,
    HashMismatch,
    FileCountMismatch
}
