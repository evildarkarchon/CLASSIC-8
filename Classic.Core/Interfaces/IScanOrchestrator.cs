using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public class ScanResult
{
    public CrashLog CrashLog { get; set; } = new();
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
}

public class BatchScanResult
{
    public List<ScanResult> Results { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessfulScans { get; set; }
    public int FailedScans { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
}

public interface IScanOrchestrator
{
    Task<ScanResult> ProcessCrashLogAsync(string logPath, CancellationToken cancellationToken = default);
    Task<BatchScanResult> ProcessCrashLogsBatchAsync(IEnumerable<string> logPaths, CancellationToken cancellationToken = default);
}
