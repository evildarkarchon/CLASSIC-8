using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Core.Enums;

namespace Classic.Avalonia.Services;

public class MockScanOrchestrator : IScanOrchestrator
{
    public async Task<ScanResult> ExecuteScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        // Mock implementation for UI development
        await Task.Delay(2000, cancellationToken); // Simulate work

        var startTime = DateTime.Now.AddSeconds(-2);
        var endTime = DateTime.Now;

        return new ScanResult
        {
            TotalLogs = 5,
            SuccessfulScans = 4,
            FailedScans = 1,
            PartialScans = 0,
            SkippedLogs = 0,
            StartTime = startTime,
            EndTime = endTime,
            UsedProcessingMode = ProcessingMode.Adaptive,
            WorkersUsed = Environment.ProcessorCount,
            ProcessedLogs = new List<string> { "crash1.log", "crash2.log", "crash3.log", "crash4.log" },
            UnsolvedLogs = new List<string> { "crash5.log" },
            OutputDirectory = request.OutputDirectory,
            OriginalRequest = request,
            Performance = new PerformanceMetrics
            {
                TotalMemoryUsed = 1024 * 1024 * 50, // 50MB
                PeakMemoryUsage = 1024 * 1024 * 75, // 75MB
                CpuUsagePercent = 25.5,
                FilesReadPerSecond = 2,
                CacheHits = 10,
                CacheMisses = 2
            },
            Summary = new ScanSummary
            {
                OverallAssessment = "Most crash logs processed successfully with minor issues detected.",
                MaxSeverity = SeverityLevel.Medium,
                TopModConflicts = new List<string> { "ModA.esp", "ModB.esp" },
                RecommendedActions = new List<string> { "Check mod load order", "Update outdated mods" }
            }
        };
    }

    public async Task<ScanLogResult> ScanSingleLogAsync(string logPath, ScanRequest? config,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        return new ScanLogResult
        {
            LogPath = logPath,
            IsSuccessful = true,
            IsPartial = false,
            ProcessingTime = DateTime.Now,
            Duration = TimeSpan.FromMilliseconds(500),
            GameId = GameId.Fallout4,
            GameVersion = "1.10.163",
            PluginCount = 45,
            FormIdCount = 123,
            SuspectCount = 2,
            IdentifiedMods = new List<string> { "ModA", "ModB" },
            ModConflicts = new List<string> { "ConflictX" },
            Suspects = new List<string> { "SuspectY", "SuspectZ" }
        };
    }

    public async Task<ScanLogResult> ScanSingleLogAsync(string logPath, CancellationToken cancellationToken = default)
    {
        return await ScanSingleLogAsync(logPath, null, cancellationToken);
    }

    public async Task<PerformanceMetrics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        return new PerformanceMetrics
        {
            TotalMemoryUsed = 1024 * 1024 * 30,
            PeakMemoryUsage = 1024 * 1024 * 45,
            CpuUsagePercent = 15.2,
            FilesReadPerSecond = 3,
            CacheHits = 25,
            CacheMisses = 5
        };
    }

    public ValidationResult ValidateRequest(ScanRequest request)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.OutputDirectory)) result.AddError("Output directory is required");

        if (request.MaxConcurrentLogs <= 0) result.AddWarning("Max concurrent logs should be greater than 0");

        return result;
    }

    public async Task<ProcessingMode> GetOptimalProcessingModeAsync(ScanRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return ProcessingMode.Adaptive;
    }

    public async Task<TimeSpan> EstimateProcessingTimeAsync(ScanRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return TimeSpan.FromMinutes(2); // Estimate 2 minutes
    }
}
