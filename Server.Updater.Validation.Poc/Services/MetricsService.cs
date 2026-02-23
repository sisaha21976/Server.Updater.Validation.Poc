using System.Diagnostics;

namespace Server.Updater.Validation.Poc.Services;

/// <summary>
/// Service for collecting and reporting metrics during update operations.
/// </summary>
internal class MetricsService
{
    private readonly Dictionary<string, OperationMetrics> _operations = [];
    private readonly Stopwatch _totalStopwatch = new();

    /// <summary>
    /// Starts tracking total execution time.
    /// </summary>
    public void StartTotal()
    {
        _totalStopwatch.Restart();
    }

    /// <summary>
    /// Measures the execution time of an operation.
    /// </summary>
    public T Measure<T>(string operationName, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = operation();
        stopwatch.Stop();

        _operations[operationName] = new OperationMetrics
        {
            OperationName = operationName,
            Duration = stopwatch.Elapsed
        };

        return result;
    }

    /// <summary>
    /// Measures the execution time of an operation (void return).
    /// </summary>
    public void Measure(string operationName, Action operation)
    {
        var stopwatch = Stopwatch.StartNew();
        operation();
        stopwatch.Stop();

        _operations[operationName] = new OperationMetrics
        {
            OperationName = operationName,
            Duration = stopwatch.Elapsed
        };
    }

    /// <summary>
    /// Measures the execution time of an operation with file count tracking.
    /// </summary>
    public T MeasureWithFileCount<T>(string operationName, int fileCount, long totalBytes, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = operation();
        stopwatch.Stop();

        _operations[operationName] = new OperationMetrics
        {
            OperationName = operationName,
            Duration = stopwatch.Elapsed,
            FileCount = fileCount,
            TotalBytes = totalBytes
        };

        return result;
    }

    /// <summary>
    /// Measures the execution time of an operation with file count tracking (void return).
    /// </summary>
    public void MeasureWithFileCount(string operationName, int fileCount, long totalBytes, Action operation)
    {
        var stopwatch = Stopwatch.StartNew();
        operation();
        stopwatch.Stop();

        _operations[operationName] = new OperationMetrics
        {
            OperationName = operationName,
            Duration = stopwatch.Elapsed,
            FileCount = fileCount,
            TotalBytes = totalBytes
        };
    }

    /// <summary>
    /// Stops total timing and prints all collected metrics to console.
    /// </summary>
    public void PrintMetrics()
    {
        _totalStopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("+------------------------------------------------------------------------------+");
        Console.WriteLine("|                           UPDATE OPERATION METRICS                           |");
        Console.WriteLine("+------------------------------------------------------------------------------+");

        foreach (var (name, metrics) in _operations)
        {
            PrintOperationMetrics(metrics);
        }

        Console.WriteLine("+------------------------------------------------------------------------------+");
        Console.WriteLine($"| {"TOTAL EXECUTION TIME",-30} | {FormatDuration(_totalStopwatch.Elapsed),44} |");
        Console.WriteLine("+------------------------------------------------------------------------------+");

        // Print validation overhead analysis
        PrintValidationOverhead();
    }

    private static void PrintOperationMetrics(OperationMetrics metrics)
    {
        Console.WriteLine($"| {metrics.OperationName,-30} | {FormatDuration(metrics.Duration),44} |");

        if (metrics.FileCount > 0)
        {
            var filesPerSecond = metrics.Duration.TotalSeconds > 0
                ? metrics.FileCount / metrics.Duration.TotalSeconds
                : 0;

            var bytesPerSecond = metrics.Duration.TotalSeconds > 0
                ? metrics.TotalBytes / metrics.Duration.TotalSeconds
                : 0;

            Console.WriteLine($"|   {"Files processed",-28} | {metrics.FileCount,44:N0} |");
            Console.WriteLine($"|   {"Total size",-28} | {FormatBytes(metrics.TotalBytes),44} |");
            Console.WriteLine($"|   {"Throughput (files/sec)",-28} | {filesPerSecond,44:N2} |");
            Console.WriteLine($"|   {"Throughput (data)",-28} | {FormatBytes((long)bytesPerSecond) + "/sec",44} |");
            Console.WriteLine($"|   {"Avg time per file",-28} | {FormatDuration(TimeSpan.FromTicks(metrics.Duration.Ticks / Math.Max(1, metrics.FileCount))),44} |");
        }

        Console.WriteLine("+------------------------------------------------------------------------------+");
    }

    private void PrintValidationOverhead()
    {
        // Only show overhead analysis when validation and copy are separate operations
        if (!_operations.TryGetValue("Validation", out var validationMetrics) ||
            !_operations.TryGetValue("File Copy", out _))
        {
            return;
        }

        var totalWithoutValidation = _totalStopwatch.Elapsed - validationMetrics.Duration;
        var overheadPercent = _totalStopwatch.Elapsed.TotalMilliseconds > 0
            ? (validationMetrics.Duration.TotalMilliseconds / _totalStopwatch.Elapsed.TotalMilliseconds) * 100
            : 0;

        Console.WriteLine();
        Console.WriteLine("+------------------------------------------------------------------------------+");
        Console.WriteLine("|                         VALIDATION OVERHEAD ANALYSIS                         |");
        Console.WriteLine("+------------------------------------------------------------------------------+");
        Console.WriteLine($"| {"Validation time",-30} | {FormatDuration(validationMetrics.Duration),44} |");
        Console.WriteLine($"| {"Time without validation",-30} | {FormatDuration(totalWithoutValidation),44} |");
        Console.WriteLine($"| {"Validation overhead",-30} | {overheadPercent,43:N2}% |");
        Console.WriteLine("+------------------------------------------------------------------------------+");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1)
        {
            return $"{duration.TotalMicroseconds:N2} ?s";
        }
        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:N2} ms";
        }
        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:N2} sec";
        }
        return $"{duration.TotalMinutes:N2} min";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:N2} {sizes[order]}";
    }
}

/// <summary>
/// Represents metrics for a single operation.
/// </summary>
internal class OperationMetrics
{
    public required string OperationName { get; init; }
    public TimeSpan Duration { get; init; }
    public int FileCount { get; init; }
    public long TotalBytes { get; init; }
}
