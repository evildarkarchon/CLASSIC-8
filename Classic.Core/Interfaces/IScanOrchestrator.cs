using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Orchestrates comprehensive crash log scanning operations
/// </summary>
public interface IScanOrchestrator
{
    /// <summary>
    /// Executes a comprehensive scan based on the provided request
    /// </summary>
    Task<ScanResult> ExecuteScanAsync(ScanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a single crash log file
    /// </summary>
    Task<ScanLogResult> ScanSingleLogAsync(string logPath, ScanRequest? config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a single crash log file with default configuration
    /// </summary>
    Task<ScanLogResult> ScanSingleLogAsync(string logPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current scanning statistics and performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a scan request configuration
    /// </summary>
    ValidationResult ValidateRequest(ScanRequest request);

    /// <summary>
    /// Gets the recommended processing mode for the given workload
    /// </summary>
    Task<ProcessingMode> GetOptimalProcessingModeAsync(ScanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the time required to process the given request
    /// </summary>
    Task<TimeSpan> EstimateProcessingTimeAsync(ScanRequest request, CancellationToken cancellationToken = default);
}
