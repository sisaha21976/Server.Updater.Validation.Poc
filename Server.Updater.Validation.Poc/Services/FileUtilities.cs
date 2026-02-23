using System.Security.Cryptography;
using System.Text.Json;

namespace Server.Updater.Validation.Poc.Services;

/// <summary>
/// Shared utilities for file operations and manifest handling.
/// </summary>
internal static class FileUtilities
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Computes SHA256 hash of a file.
    /// </summary>
    public static string ComputeSha256Hash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Serializes an object to JSON and writes it to a file.
    /// </summary>
    public static void WriteJson<T>(string filePath, T obj)
    {
        var jsonContent = JsonSerializer.Serialize(obj, JsonOptions);
        File.WriteAllText(filePath, jsonContent);
    }

    /// <summary>
    /// Reads and deserializes JSON from a file.
    /// </summary>
    public static T? ReadJson<T>(string filePath)
    {
        var jsonContent = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(jsonContent, JsonOptions);
    }

    /// <summary>
    /// Gets the full path for a zip entry, with zip slip vulnerability protection.
    /// </summary>
    public static string GetSafeFullPath(string entryFullName, string rootPath)
    {
        string fileName = Path.Combine(rootPath, entryFullName);
        string fullPath = Path.GetFullPath(fileName);

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Attempt to extract file outside target directory: {fullPath}");
        }

        return fullPath;
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Cleans and recreates a directory.
    /// </summary>
    public static void CleanDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// Copies a file, creating the destination directory if needed.
    /// </summary>
    public static void CopyFileWithDirectory(string sourcePath, string destinationPath)
    {
        var directoryPath = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            EnsureDirectoryExists(directoryPath);
        }
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}
