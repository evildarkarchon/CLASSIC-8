using Classic.Core.Enums;

namespace Classic.Core.Models;

/// <summary>
/// Comprehensive result of crash log scanning operations
/// </summary>
public class ScanResult
{
    // Basic Statistics
    public int TotalLogs { get; set; }
    public int SuccessfulScans { get; set; }
    public int FailedScans { get; set; }
    public int PartialScans { get; set; }
    public int SkippedLogs { get; set; }
    
    // Processing Information
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan ProcessingTime => EndTime - StartTime;
    public ProcessingMode UsedProcessingMode { get; set; }
    public int WorkersUsed { get; set; }
    
    // File Lists
    public List<string> ProcessedLogs { get; set; } = new();
    public List<string> UnsolvedLogs { get; set; } = new();
    public List<string> CorruptedLogs { get; set; } = new();
    public List<string> MovedLogs { get; set; } = new();
    
    // Error Information
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, List<string>> LogSpecificErrors { get; set; } = new();
    
    // Analysis Results
    public List<ScanLogResult> DetailedResults { get; set; } = new();
    public ScanSummary Summary { get; set; } = new();
    
    // Performance Metrics
    public PerformanceMetrics Performance { get; set; } = new();
    
    // Output Information
    public List<string> GeneratedReports { get; set; } = new();
    public string? SummaryReportPath { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    
    // Statistics
    public Dictionary<string, object> Statistics { get; set; } = new();
    public Dictionary<GameId, int> GameDistribution { get; set; } = new();
    public Dictionary<string, int> ErrorTypes { get; set; } = new();
    public Dictionary<string, int> ModConflicts { get; set; } = new();
    
    // Configuration Used
    public ScanRequest? OriginalRequest { get; set; }
    
    /// <summary>
    /// Gets the success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalLogs > 0 ? (double)SuccessfulScans / TotalLogs * 100 : 0;
    
    /// <summary>
    /// Gets the failure rate as a percentage
    /// </summary>
    public double FailureRate => TotalLogs > 0 ? (double)FailedScans / TotalLogs * 100 : 0;
    
    /// <summary>
    /// Gets the average processing time per log
    /// </summary>
    public TimeSpan AverageProcessingTime => TotalLogs > 0 ? 
        TimeSpan.FromMilliseconds(ProcessingTime.TotalMilliseconds / TotalLogs) : 
        TimeSpan.Zero;
    
    /// <summary>
    /// Indicates if the scan completed successfully overall
    /// </summary>
    public bool IsSuccessful => FailedScans == 0 && Errors.Count == 0;
    
    /// <summary>
    /// Indicates if there were any warnings
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
    
    /// <summary>
    /// Adds a detailed result for a specific log file
    /// </summary>
    public void AddLogResult(ScanLogResult result)
    {
        DetailedResults.Add(result);
        ProcessedLogs.Add(result.LogPath);
        
        if (result.IsSuccessful)
        {
            SuccessfulScans++;
        }
        else if (result.IsPartial)
        {
            PartialScans++;
        }
        else
        {
            FailedScans++;
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                LogSpecificErrors[result.LogPath] = new List<string> { result.ErrorMessage };
            }
        }
        
        // Update game distribution
        if (result.GameId.HasValue)
        {
            GameDistribution.TryGetValue(result.GameId.Value, out var count);
            GameDistribution[result.GameId.Value] = count + 1;
        }
        
        // Update mod conflicts
        foreach (var conflict in result.ModConflicts)
        {
            ModConflicts.TryGetValue(conflict, out var conflictCount);
            ModConflicts[conflict] = conflictCount + 1;
        }
    }
    
    /// <summary>
    /// Adds an error to the result
    /// </summary>
    public void AddError(string error, string? logPath = null)
    {
        Errors.Add(error);
        
        if (!string.IsNullOrEmpty(logPath))
        {
            if (!LogSpecificErrors.ContainsKey(logPath))
                LogSpecificErrors[logPath] = new List<string>();
            LogSpecificErrors[logPath].Add(error);
        }
    }
    
    /// <summary>
    /// Adds a warning to the result
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
    
    /// <summary>
    /// Generates a summary report of the scan results
    /// </summary>
    public string GenerateTextSummary()
    {
        var summary = new System.Text.StringBuilder();
        
        summary.AppendLine("CLASSIC-8 Scan Results Summary");
        summary.AppendLine("==============================");
        summary.AppendLine();
        
        summary.AppendLine("Overall Statistics:");
        summary.AppendLine($"  Total logs processed: {TotalLogs}");
        summary.AppendLine($"  Successful scans: {SuccessfulScans} ({SuccessRate:F1}%)");
        summary.AppendLine($"  Failed scans: {FailedScans} ({FailureRate:F1}%)");
        summary.AppendLine($"  Partial scans: {PartialScans}");
        summary.AppendLine($"  Skipped logs: {SkippedLogs}");
        summary.AppendLine();
        
        summary.AppendLine("Performance:");
        summary.AppendLine($"  Processing time: {ProcessingTime:mm\\:ss}");
        summary.AppendLine($"  Average per log: {AverageProcessingTime.TotalSeconds:F2}s");
        summary.AppendLine($"  Processing mode: {UsedProcessingMode}");
        summary.AppendLine($"  Workers used: {WorkersUsed}");
        summary.AppendLine();
        
        if (GameDistribution.Count > 0)
        {
            summary.AppendLine("Game Distribution:");
            foreach (var (game, count) in GameDistribution.OrderByDescending(x => x.Value))
            {
                summary.AppendLine($"  {game}: {count} logs");
            }
            summary.AppendLine();
        }
        
        if (ModConflicts.Count > 0)
        {
            summary.AppendLine("Top Mod Conflicts:");
            foreach (var (mod, count) in ModConflicts.OrderByDescending(x => x.Value).Take(10))
            {
                summary.AppendLine($"  {mod}: {count} occurrences");
            }
            summary.AppendLine();
        }
        
        if (Errors.Count > 0)
        {
            summary.AppendLine("Errors:");
            foreach (var error in Errors.Take(10))
            {
                summary.AppendLine($"  - {error}");
            }
            if (Errors.Count > 10)
                summary.AppendLine($"  ... and {Errors.Count - 10} more");
            summary.AppendLine();
        }
        
        if (Warnings.Count > 0)
        {
            summary.AppendLine("Warnings:");
            foreach (var warning in Warnings.Take(5))
            {
                summary.AppendLine($"  - {warning}");
            }
            if (Warnings.Count > 5)
                summary.AppendLine($"  ... and {Warnings.Count - 5} more");
        }
        
        return summary.ToString();
    }
}

/// <summary>
/// Result for a single crash log scan
/// </summary>
public class ScanLogResult
{
    public string LogPath { get; set; } = string.Empty;
    public string LogFileName => Path.GetFileName(LogPath);
    public bool IsSuccessful { get; set; }
    public bool IsPartial { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessingTime { get; set; }
    public TimeSpan Duration { get; set; }
    
    // Crash Log Information
    public GameId? GameId { get; set; }
    public string? GameVersion { get; set; }
    public string? CrashGenVersion { get; set; }
    public DateTime? CrashDate { get; set; }
    public string? MainError { get; set; }
    
    // Analysis Results
    public int PluginCount { get; set; }
    public int FormIdCount { get; set; }
    public int SuspectCount { get; set; }
    public int RecordCount { get; set; }
    public List<string> IdentifiedMods { get; set; } = new();
    public List<string> ModConflicts { get; set; } = new();
    public List<string> Suspects { get; set; } = new();
    
    // Output Files
    public string? ReportPath { get; set; }
    public string? BackupPath { get; set; }
    
    // Performance
    public Dictionary<string, TimeSpan> AnalyzerTimes { get; set; } = new();
    public long MemoryUsed { get; set; }
}

/// <summary>
/// High-level summary of scan results
/// </summary>
public class ScanSummary
{
    public Dictionary<string, int> ErrorCategories { get; set; } = new();
    public Dictionary<string, int> SuspectCategories { get; set; } = new();
    public List<string> TopModConflicts { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public string? OverallAssessment { get; set; }
    public SeverityLevel MaxSeverity { get; set; } = SeverityLevel.Low;
}

/// <summary>
/// Enhanced performance metrics for the scan operation with real-time monitoring
/// </summary>
public class PerformanceMetrics
{
    // Memory Metrics
    public long TotalMemoryUsed { get; set; }
    public long PeakMemoryUsage { get; set; }
    public long CurrentMemoryUsage { get; set; }
    public long GCCollections { get; set; }
    public long GCMemoryReleased { get; set; }
    
    // CPU Metrics
    public double CpuUsagePercent { get; set; }
    public double PeakCpuUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public int CpuCores { get; set; }
    
    // Throughput Metrics
    public int FilesReadPerSecond { get; set; }
    public int PeakThroughput { get; set; }
    public int CurrentThroughput { get; set; }
    public long TotalBytesRead { get; set; }
    public double MegabytesPerSecond { get; set; }
    
    // Processing Metrics
    public Dictionary<string, TimeSpan> ComponentTimes { get; set; } = new();
    public Dictionary<ProcessingMode, TimeSpan> StrategyTimes { get; set; } = new();
    public Dictionary<string, int> ComponentCounts { get; set; } = new();
    public Dictionary<string, long> ComponentMemoryUsage { get; set; } = new();
    
    // Caching Metrics
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public long CacheMemoryUsage { get; set; }
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? 
        (double)CacheHits / (CacheHits + CacheMisses) * 100 : 0;
    
    // Worker Thread Metrics
    public int TotalFilesProcessed { get; set; }
    public int WorkerThreadsUsed { get; set; }
    public int OptimalWorkerThreads { get; set; }
    public int IdleWorkerThreads { get; set; }
    public double WorkerEfficiency { get; set; }
    
    // Database/IO Metrics
    public int DatabaseQueries { get; set; }
    public TimeSpan TotalDatabaseTime { get; set; }
    public int FileSystemOperations { get; set; }
    public TimeSpan TotalFileIOTime { get; set; }
    
    // Real-time Tracking
    public DateTime StartTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public List<PerformanceSnapshot> Snapshots { get; set; } = new();
    
    // Bottleneck Detection
    public List<string> DetectedBottlenecks { get; set; } = new();
    public string? PrimaryBottleneck { get; set; }
    public double BottleneckSeverity { get; set; }
    
    // Adaptive Processing Data
    public Dictionary<ProcessingMode, double> ModeEfficiencies { get; set; } = new();
    public int ProcessingModeChanges { get; set; }
    public ProcessingMode OptimalMode { get; set; }
    
    /// <summary>
    /// Calculates the overall system efficiency score (0-100)
    /// </summary>
    public double OverallEfficiency
    {
        get
        {
            var factors = new List<double>();
            
            // CPU efficiency (inverse of usage - lower is better for efficiency)
            if (AverageCpuUsage > 0)
                factors.Add(Math.Max(0, 100 - AverageCpuUsage));
            
            // Memory efficiency
            if (PeakMemoryUsage > 0)
                factors.Add(Math.Max(0, 100 - (CurrentMemoryUsage / (double)PeakMemoryUsage * 100)));
            
            // Cache efficiency
            factors.Add(CacheHitRate);
            
            // Worker efficiency
            factors.Add(WorkerEfficiency);
            
            return factors.Count > 0 ? factors.Average() : 0;
        }
    }
    
    /// <summary>
    /// Gets the current processing rate in files per second
    /// </summary>
    public double CurrentProcessingRate
    {
        get
        {
            var elapsed = DateTime.Now - StartTime;
            return elapsed.TotalSeconds > 0 ? TotalFilesProcessed / elapsed.TotalSeconds : 0;
        }
    }
    
    /// <summary>
    /// Records a performance snapshot for trend analysis
    /// </summary>
    public void RecordSnapshot()
    {
        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.Now,
            MemoryUsage = CurrentMemoryUsage,
            CpuUsage = CpuUsagePercent,
            Throughput = CurrentThroughput,
            FilesProcessed = TotalFilesProcessed,
            WorkerThreadsActive = WorkerThreadsUsed - IdleWorkerThreads
        };
        
        Snapshots.Add(snapshot);
        LastUpdateTime = DateTime.Now;
        
        // Keep only last 1000 snapshots to prevent memory bloat
        if (Snapshots.Count > 1000)
        {
            Snapshots.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Detects performance bottlenecks based on current metrics
    /// </summary>
    public void DetectBottlenecks()
    {
        DetectedBottlenecks.Clear();
        
        // CPU bottleneck
        if (AverageCpuUsage > 85)
        {
            DetectedBottlenecks.Add("CPU");
        }
        
        // Memory bottleneck
        if (CurrentMemoryUsage > PeakMemoryUsage * 0.9)
        {
            DetectedBottlenecks.Add("Memory");
        }
        
        // Cache bottleneck
        if (CacheHitRate < 70)
        {
            DetectedBottlenecks.Add("Cache");
        }
        
        // Worker thread bottleneck
        if (WorkerEfficiency < 60)
        {
            DetectedBottlenecks.Add("Worker Threads");
        }
        
        // IO bottleneck
        if (TotalFileIOTime.TotalMilliseconds > ComponentTimes.Values.Sum(t => t.TotalMilliseconds) * 0.5)
        {
            DetectedBottlenecks.Add("File I/O");
        }
        
        // Database bottleneck
        if (TotalDatabaseTime.TotalMilliseconds > ComponentTimes.Values.Sum(t => t.TotalMilliseconds) * 0.3)
        {
            DetectedBottlenecks.Add("Database");
        }
        
        // Determine primary bottleneck
        PrimaryBottleneck = DetectedBottlenecks.FirstOrDefault();
        BottleneckSeverity = DetectedBottlenecks.Count * 20; // Simple severity calculation
    }
}

/// <summary>
/// Performance snapshot for trend analysis
/// </summary>
public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public int Throughput { get; set; }
    public int FilesProcessed { get; set; }
    public int WorkerThreadsActive { get; set; }
}