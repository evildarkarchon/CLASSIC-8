using Classic.Core.Enums;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using CoreScanResult = Classic.Core.Models.ScanResult;

namespace Classic.ScanLog.Utilities;

/// <summary>
/// Maps analysis results to advanced report data structure
/// </summary>
public class ReportDataMapper
{
    private readonly ILogger<ReportDataMapper> _logger;

    public ReportDataMapper(ILogger<ReportDataMapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts scan results to advanced report data
    /// </summary>
    public AdvancedReportData MapScanResultToReportData(
        CoreScanResult scanResult, 
        string fileName,
        string classicVersion = "1.0.0")
    {
        try
        {
            var reportData = new AdvancedReportData
            {
                FileName = fileName,
                ClassicVersion = classicVersion,
                GeneratedDate = DateTime.Now,
                ProcessingTime = scanResult.ProcessingTime
            };

            // Map crash log data
            if (scanResult.DetailedResults.Any())
            {
                var firstResult = scanResult.DetailedResults.First();
                MapCrashLogData(firstResult, reportData);
            }

            // Map performance data
            MapPerformanceData(scanResult, reportData);

            // Categorize issues
            CategorizeIssues(scanResult, reportData);

            _logger.LogDebug("Successfully mapped scan result to report data for {FileName}", fileName);
            return reportData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map scan result to report data for {FileName}", fileName);
            return CreateFallbackReportData(fileName, classicVersion);
        }
    }

    /// <summary>
    /// Maps individual crash log analysis to report data
    /// </summary>
    public AdvancedReportData MapCrashLogAnalysisToReportData(
        CrashLog crashLog,
        List<Suspect> suspects,
        List<ModConflictResult> modConflicts,
        List<FileValidationResult> fileValidationResults,
        string fileName,
        TimeSpan processingTime,
        string classicVersion = "1.0.0")
    {
        try
        {
            var reportData = new AdvancedReportData
            {
                FileName = fileName,
                ClassicVersion = classicVersion,
                GeneratedDate = DateTime.Now,
                ProcessingTime = processingTime,
                CrashLog = crashLog,
                CrashSuspects = suspects,
                ModConflicts = modConflicts,
                FileValidationResults = fileValidationResults
            };

            // Extract system info from crash log
            MapSystemInfoFromCrashLog(crashLog, reportData);

            // Extract game info
            MapGameInfoFromCrashLog(crashLog, reportData);

            // Map plugins
            reportData.ProblematicPlugins = crashLog.Plugins
                .Where(p => HasPluginIssues(p))
                .ToList();

            // Categorize all issues
            CategorizeAllIssues(reportData);

            _logger.LogDebug("Successfully mapped crash log analysis to report data for {FileName}", fileName);
            return reportData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map crash log analysis to report data for {FileName}", fileName);
            return CreateFallbackReportData(fileName, classicVersion);
        }
    }

    /// <summary>
    /// Maps crash log data to report structure
    /// </summary>
    private void MapCrashLogData(ScanLogResult logResult, AdvancedReportData reportData)
    {
        reportData.GameInfo = new GameInfo
        {
            GameId = logResult.GameId ?? GameId.Unknown,
            GameVersion = logResult.GameVersion ?? "Unknown",
            CrashGenName = ExtractCrashGenName(logResult.CrashGenVersion),
            CrashGenVersion = logResult.CrashGenVersion ?? "Unknown",
            IsOutdated = false // TODO: Implement version checking
        };

        reportData.CrashLog = new CrashLog
        {
            FileName = logResult.LogFileName,
            MainError = logResult.MainError ?? string.Empty,
            DateCreated = logResult.CrashDate ?? DateTime.MinValue,
            GameVersion = logResult.GameVersion ?? "Unknown",
            CrashGenVersion = logResult.CrashGenVersion ?? "Unknown"
        };

        // Map suspects
        reportData.CrashSuspects = logResult.Suspects.Select(suspectName => new Suspect
        {
            Name = suspectName,
            Description = "Detected from crash analysis",
            SeverityScore = 3, // Default severity
            Evidence = "Found in crash log analysis",
            Recommendation = "Check mod compatibility and load order"
        }).ToList();

        // Map mod conflicts
        reportData.ModConflicts = logResult.ModConflicts.Select(conflictText => 
            ParseModConflictText(conflictText)).ToList();
    }

    /// <summary>
    /// Maps performance data from scan result
    /// </summary>
    private void MapPerformanceData(CoreScanResult scanResult, AdvancedReportData reportData)
    {
        reportData.Performance = scanResult.Performance ?? new PerformanceMetrics
        {
            TotalFilesProcessed = scanResult.TotalLogs,
            WorkerThreadsUsed = scanResult.WorkersUsed
        };
    }

    /// <summary>
    /// Extracts system information from crash log segments
    /// </summary>
    private void MapSystemInfoFromCrashLog(CrashLog crashLog, AdvancedReportData reportData)
    {
        if (crashLog.Segments.TryGetValue("SystemSpecs", out var systemSpecs))
        {
            var systemInfo = new SystemInfo();
            
            foreach (var line in systemSpecs)
            {
                if (line.Contains("OS:", StringComparison.OrdinalIgnoreCase))
                {
                    systemInfo.OperatingSystem = ExtractValue(line, "OS:");
                }
                else if (line.Contains("CPU:", StringComparison.OrdinalIgnoreCase))
                {
                    systemInfo.Cpu = ExtractValue(line, "CPU:");
                }
                else if (line.Contains("GPU", StringComparison.OrdinalIgnoreCase))
                {
                    systemInfo.Gpu = ExtractValue(line, "GPU #1:");
                    systemInfo.GpuManufacturer = DetectGpuManufacturer(systemInfo.Gpu);
                }
                else if (line.Contains("MEMORY:", StringComparison.OrdinalIgnoreCase))
                {
                    systemInfo.Memory = ExtractValue(line, "PHYSICAL MEMORY:");
                }
            }
            
            reportData.SystemInfo = systemInfo;
        }
    }

    /// <summary>
    /// Maps game information from crash log
    /// </summary>
    private void MapGameInfoFromCrashLog(CrashLog crashLog, AdvancedReportData reportData)
    {
        reportData.GameInfo = new GameInfo
        {
            GameId = DetectGameFromVersion(crashLog.GameVersion),
            GameVersion = crashLog.GameVersion,
            CrashGenName = ExtractCrashGenName(crashLog.CrashGenVersion),
            CrashGenVersion = crashLog.CrashGenVersion,
            IsOutdated = false // TODO: Implement version checking
        };
    }

    /// <summary>
    /// Categorizes issues from scan result
    /// </summary>
    private void CategorizeIssues(CoreScanResult scanResult, AdvancedReportData reportData)
    {
        // Critical issues
        if (scanResult.FailedScans > scanResult.TotalLogs * 0.5)
        {
            reportData.CriticalIssues.Add(new ReportIssue
            {
                Type = ReportIssueType.SystemCompatibility,
                Title = "High Failure Rate Detected",
                Description = $"More than 50% of logs failed to process ({scanResult.FailedScans}/{scanResult.TotalLogs})",
                Severity = 5,
                Recommendation = "Check for corrupted log files or system compatibility issues"
            });
        }

        // Warnings from errors
        foreach (var error in scanResult.Errors)
        {
            reportData.Warnings.Add(new ReportIssue
            {
                Type = ReportIssueType.GameConfiguration,
                Title = "Processing Warning",
                Description = error,
                Severity = 2
            });
        }

        // Performance recommendations
        if (scanResult.ProcessingTime.TotalMinutes > 5)
        {
            reportData.Recommendations.Add(new ReportIssue
            {
                Type = ReportIssueType.PerformanceIssue,
                Title = "Slow Processing Detected",
                Description = $"Processing took {scanResult.ProcessingTime.TotalMinutes:F1} minutes",
                Severity = 2,
                Recommendation = "Consider using parallel processing mode for better performance"
            });
        }
    }

    /// <summary>
    /// Categorizes all issues from various analysis results
    /// </summary>
    private void CategorizeAllIssues(AdvancedReportData reportData)
    {
        // Categorize crash suspects by severity
        foreach (var suspect in reportData.CrashSuspects)
        {
            var issue = new ReportIssue
            {
                Type = ReportIssueType.CrashSuspect,
                Title = suspect.Name,
                Description = suspect.Description,
                Recommendation = suspect.Recommendation,
                Severity = suspect.SeverityScore
            };

            if (suspect.SeverityScore >= 5)
                reportData.CriticalIssues.Add(issue);
            else if (suspect.SeverityScore >= 4)
                reportData.Errors.Add(issue);
            else
                reportData.Warnings.Add(issue);
        }

        // Categorize mod conflicts
        foreach (var modConflict in reportData.ModConflicts)
        {
            var issue = new ReportIssue
            {
                Type = ReportIssueType.ModConflict,
                Title = modConflict.ModName,
                Description = modConflict.Warning,
                Recommendation = modConflict.Solution,
                Severity = GetModConflictSeverity(modConflict.Type)
            };

            if (modConflict.Type == ConflictType.FrequentCrash)
                reportData.Errors.Add(issue);
            else
                reportData.Warnings.Add(issue);
        }

        // Categorize file validation issues
        foreach (var fileIssue in reportData.FileValidationResults.Where(f => f.Status != ValidationStatus.Valid))
        {
            var issue = new ReportIssue
            {
                Type = ReportIssueType.FileValidation,
                Title = $"File Issue: {fileIssue.RelativePath}",
                Description = fileIssue.Issue,
                Recommendation = fileIssue.Recommendation,
                Severity = GetValidationSeverity(fileIssue.Status),
                AffectedFiles = { fileIssue.FilePath }
            };

            switch (fileIssue.Status)
            {
                case ValidationStatus.Critical:
                    reportData.CriticalIssues.Add(issue);
                    break;
                case ValidationStatus.Error:
                    reportData.Errors.Add(issue);
                    break;
                default:
                    reportData.Warnings.Add(issue);
                    break;
            }
        }
    }

    // Helper methods

    private ModConflictResult ParseModConflictText(string conflictText)
    {
        // Parse conflict text like "[FrequentCrash] ModName: Description"
        var parts = conflictText.Split(new[] { "] ", ": " }, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 3)
        {
            var typeString = parts[0].TrimStart('[');
            var conflictType = Enum.TryParse<ConflictType>(typeString, out var parsedType) 
                ? parsedType 
                : ConflictType.ModPairConflict;
            
            return new ModConflictResult
            {
                Type = conflictType,
                ModName = parts[1],
                Warning = parts[2],
                Solution = parts.Length > 3 ? parts[3] : string.Empty
            };
        }
        
        return new ModConflictResult
        {
            Type = ConflictType.ModPairConflict,
            ModName = "Unknown",
            Warning = conflictText,
            Solution = string.Empty
        };
    }

    private string ExtractCrashGenName(string crashGenVersion)
    {
        if (string.IsNullOrEmpty(crashGenVersion))
            return "Unknown";
            
        if (crashGenVersion.Contains("Buffout", StringComparison.OrdinalIgnoreCase))
            return "Buffout 4";
        if (crashGenVersion.Contains("Crash Logger", StringComparison.OrdinalIgnoreCase))
            return "Crash Logger";
            
        return "Crash Reporter";
    }

    private GameId DetectGameFromVersion(string gameVersion)
    {
        if (string.IsNullOrEmpty(gameVersion))
            return GameId.Unknown;
            
        if (gameVersion.Contains("Fallout 4", StringComparison.OrdinalIgnoreCase))
            return GameId.Fallout4;
        if (gameVersion.Contains("Skyrim", StringComparison.OrdinalIgnoreCase))
            return GameId.SkyrimSE;
            
        return GameId.Unknown;
    }

    private GpuManufacturer DetectGpuManufacturer(string gpuInfo)
    {
        if (string.IsNullOrEmpty(gpuInfo))
            return GpuManufacturer.Unknown;
            
        var gpuLower = gpuInfo.ToLowerInvariant();
        
        if (gpuLower.Contains("nvidia") || gpuLower.Contains("geforce") || gpuLower.Contains("gtx") || gpuLower.Contains("rtx"))
            return GpuManufacturer.Nvidia;
        if (gpuLower.Contains("amd") || gpuLower.Contains("radeon") || gpuLower.Contains("rx"))
            return GpuManufacturer.Amd;
        if (gpuLower.Contains("intel") || gpuLower.Contains("uhd") || gpuLower.Contains("iris"))
            return GpuManufacturer.Intel;
            
        return GpuManufacturer.Unknown;
    }

    private bool HasPluginIssues(PluginInfo plugin)
    {
        // Simple heuristic - could be enhanced with more sophisticated analysis
        return plugin.HasPluginLimit || 
               plugin.FileName.Contains("crash", StringComparison.OrdinalIgnoreCase) ||
               plugin.LoadOrder >= 254;
    }

    private string ExtractValue(string line, string prefix)
    {
        var index = line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            return line.Substring(index + prefix.Length).Trim();
        }
        return string.Empty;
    }

    private int GetModConflictSeverity(ConflictType conflictType) => conflictType switch
    {
        ConflictType.FrequentCrash => 4,
        ConflictType.ModPairConflict => 3,
        ConflictType.MissingImportant => 2,
        ConflictType.HasSolution => 1,
        ConflictType.GpuIncompatible => 3,
        ConflictType.LoadOrderIssue => 2,
        _ => 2
    };

    private int GetValidationSeverity(ValidationStatus status) => status switch
    {
        ValidationStatus.Critical => 5,
        ValidationStatus.Error => 4,
        ValidationStatus.Warning => 2,
        ValidationStatus.Valid => 0,
        _ => 2
    };

    /// <summary>
    /// Creates a minimal fallback report data when mapping fails
    /// </summary>
    private AdvancedReportData CreateFallbackReportData(string fileName, string classicVersion)
    {
        return new AdvancedReportData
        {
            FileName = fileName,
            ClassicVersion = classicVersion,
            GeneratedDate = DateTime.Now,
            Errors = new List<ReportIssue>
            {
                new()
                {
                    Type = ReportIssueType.SystemCompatibility,
                    Title = "Report Generation Error",
                    Description = "An error occurred while generating the comprehensive report",
                    Severity = 3
                }
            }
        };
    }
}