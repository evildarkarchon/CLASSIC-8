using Microsoft.Extensions.Logging;
using Classic.Core.Models;
using Classic.ScanLog.Services;
using System.Diagnostics;

namespace Classic.ScanLog.Services;

/// <summary>
/// Advanced adaptive processing engine that dynamically optimizes processing strategies
/// based on real-time performance metrics and system resources
/// </summary>
public class AdaptiveProcessingEngine
{
    private readonly ILogger<AdaptiveProcessingEngine> _logger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ResourceManager _resourceManager;
    
    // Performance history for decision making
    private readonly List<ProcessingPerformanceRecord> _performanceHistory = new();
    private readonly Dictionary<ProcessingMode, ProcessingModeStats> _modeStats = new();
    private readonly object _lock = new();
    
    // Adaptive parameters
    private const int MaxHistoryRecords = 50;
    private const double PerformanceThreshold = 0.8; // 80% efficiency threshold
    private const double MemoryPressureThreshold = 0.75; // 75% memory usage threshold
    private const double CpuPressureThreshold = 0.85; // 85% CPU usage threshold
    
    public AdaptiveProcessingEngine(
        ILogger<AdaptiveProcessingEngine> logger,
        PerformanceMonitor performanceMonitor,
        ResourceManager resourceManager)
    {
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _resourceManager = resourceManager;
        
        // Initialize mode statistics
        foreach (ProcessingMode mode in Enum.GetValues<ProcessingMode>())
        {
            _modeStats[mode] = new ProcessingModeStats { Mode = mode };
        }
        
        _logger.LogInformation("Adaptive processing engine initialized");
    }
    
    /// <summary>
    /// Determines the optimal processing mode based on current conditions
    /// </summary>
    public async Task<ProcessingMode> GetOptimalProcessingModeAsync(
        ScanRequest request, 
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                // Get current system state
                var resourceUsage = _resourceManager.GetResourceUsage();
                var performanceMetrics = _performanceMonitor.GetStatisticsAsync().Result;
                
                // Create decision context
                var context = new ProcessingDecisionContext
                {
                    FileCount = request.LogFiles.Count,
                    MemoryUsagePercent = resourceUsage.MemoryUsagePercent,
                    CpuUsagePercent = performanceMetrics.CpuUsagePercent,
                    AvailableWorkerThreads = resourceUsage.AvailableWorkerThreads,
                    IsUnderMemoryPressure = resourceUsage.IsUnderMemoryPressure,
                    IsUnderCpuPressure = resourceUsage.IsUnderCpuPressure,
                    HasPerformanceHistory = _performanceHistory.Count > 0,
                    AverageFileSize = CalculateAverageFileSize(request.LogFiles),
                    SystemLoad = CalculateSystemLoad(performanceMetrics, resourceUsage)
                };
                
                var optimalMode = SelectOptimalMode(context);
                
                _logger.LogInformation("Selected processing mode: {Mode} for {FileCount} files " +
                    "(Memory: {MemoryPercent:F1}%, CPU: {CpuPercent:F1}%, Load: {SystemLoad:F2})",
                    optimalMode, context.FileCount, context.MemoryUsagePercent, 
                    context.CpuUsagePercent, context.SystemLoad);
                
                return optimalMode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining optimal processing mode, falling back to default");
                return GetFallbackMode(request.LogFiles.Count);
            }
        }
    }
    
    /// <summary>
    /// Calculates optimal worker thread count based on current conditions
    /// </summary>
    public int CalculateOptimalWorkerCount(ProcessingMode mode, ScanRequest request)
    {
        var resourceUsage = _resourceManager.GetResourceUsage();
        var baseWorkerCount = Environment.ProcessorCount;
        
        // Adjust based on processing mode
        var modeMultiplier = mode switch
        {
            ProcessingMode.Sequential => 1,
            ProcessingMode.Parallel => 1.5,
            ProcessingMode.ProducerConsumer => 2.0,
            ProcessingMode.Adaptive => 1.8,
            _ => 1.0
        };
        
        var optimalCount = (int)(baseWorkerCount * modeMultiplier);
        
        // Adjust for system pressure
        if (resourceUsage.IsUnderMemoryPressure)
        {
            optimalCount = Math.Max(1, optimalCount / 2);
            _logger.LogDebug("Reduced worker count due to memory pressure: {Count}", optimalCount);
        }
        
        if (resourceUsage.IsUnderCpuPressure)
        {
            optimalCount = Math.Max(1, optimalCount / 2);
            _logger.LogDebug("Reduced worker count due to CPU pressure: {Count}", optimalCount);
        }
        
        // Adjust for file count
        if (request.LogFiles.Count < optimalCount)
        {
            optimalCount = request.LogFiles.Count;
        }
        
        // Respect system limits
        optimalCount = Math.Min(optimalCount, resourceUsage.MaxWorkerThreads);
        
        return Math.Max(1, optimalCount);
    }
    
    /// <summary>
    /// Calculates optimal batch size based on current conditions
    /// </summary>
    public int CalculateOptimalBatchSize(ProcessingMode mode, ScanRequest request)
    {
        var resourceUsage = _resourceManager.GetResourceUsage();
        var baseBatchSize = request.BatchSize;
        
        // Adjust based on memory pressure
        if (resourceUsage.MemoryUsagePercent > 80)
        {
            baseBatchSize = Math.Max(10, baseBatchSize / 4);
        }
        else if (resourceUsage.MemoryUsagePercent > 60)
        {
            baseBatchSize = Math.Max(25, baseBatchSize / 2);
        }
        
        // Adjust based on processing mode
        var adjustedBatchSize = mode switch
        {
            ProcessingMode.Sequential => Math.Min(baseBatchSize, 50),
            ProcessingMode.Parallel => baseBatchSize,
            ProcessingMode.ProducerConsumer => Math.Max(baseBatchSize, 100),
            ProcessingMode.Adaptive => baseBatchSize,
            _ => baseBatchSize
        };
        
        // Ensure reasonable bounds
        return Math.Max(1, Math.Min(adjustedBatchSize, 1000));
    }
    
    /// <summary>
    /// Records performance data for a completed processing operation
    /// </summary>
    public void RecordPerformanceData(ProcessingMode mode, ProcessingPerformanceData data)
    {
        lock (_lock)
        {
            // Update mode statistics
            if (_modeStats.TryGetValue(mode, out var stats))
            {
                stats.TotalRuns++;
                stats.TotalProcessingTime += data.ProcessingTime;
                stats.TotalFilesProcessed += data.FilesProcessed;
                stats.TotalMemoryUsed += data.PeakMemoryUsage;
                
                // Update efficiency metrics
                var efficiency = CalculateEfficiency(data);
                stats.EfficiencySum += efficiency;
                stats.AverageEfficiency = stats.EfficiencySum / stats.TotalRuns;
                
                if (efficiency > stats.BestEfficiency)
                {
                    stats.BestEfficiency = efficiency;
                    stats.BestPerformanceContext = data.Context;
                }
                
                if (efficiency < stats.WorstEfficiency)
                {
                    stats.WorstEfficiency = efficiency;
                }
                
                // Update recent performance
                stats.RecentEfficiencies.Add(efficiency);
                if (stats.RecentEfficiencies.Count > 10)
                {
                    stats.RecentEfficiencies.RemoveAt(0);
                }
                
                stats.RecentAverageEfficiency = stats.RecentEfficiencies.Average();
            }
            
            // Add to performance history
            var record = new ProcessingPerformanceRecord
            {
                Timestamp = DateTime.UtcNow,
                Mode = mode,
                Data = data,
                Efficiency = CalculateEfficiency(data)
            };
            
            _performanceHistory.Add(record);
            
            // Trim history if needed
            if (_performanceHistory.Count > MaxHistoryRecords)
            {
                _performanceHistory.RemoveAt(0);
            }
            
            _logger.LogDebug("Recorded performance data for {Mode}: {Efficiency:F2}% efficiency, " +
                "{FilesPerSecond:F2} files/sec, {MemoryMB:F0}MB peak",
                mode, record.Efficiency, data.FilesPerSecond, data.PeakMemoryUsage / 1024.0 / 1024.0);
        }
    }
    
    /// <summary>
    /// Monitors processing performance and suggests adaptations
    /// </summary>
    public async Task<ProcessingAdaptation?> MonitorAndAdaptAsync(
        ProcessingMode currentMode, 
        ProcessingPerformanceData currentData,
        CancellationToken cancellationToken = default)
    {
        var currentEfficiency = CalculateEfficiency(currentData);
        
        // Check if current performance is below threshold
        if (currentEfficiency < PerformanceThreshold)
        {
            var resourceUsage = _resourceManager.GetResourceUsage();
            var recommendations = _resourceManager.GetProcessingRecommendations();
            
            // Determine if we should switch modes
            var suggestedMode = recommendations.SuggestedProcessingMode;
            
            if (suggestedMode != currentMode)
            {
                _logger.LogInformation("Performance below threshold ({Efficiency:F2}%), " +
                    "suggesting switch from {CurrentMode} to {SuggestedMode}",
                    currentEfficiency, currentMode, suggestedMode);
                
                return new ProcessingAdaptation
                {
                    SuggestedMode = suggestedMode,
                    SuggestedWorkerCount = recommendations.SuggestedMaxWorkers,
                    SuggestedBatchSize = recommendations.SuggestedBatchSize,
                    Reason = $"Performance below threshold: {currentEfficiency:F2}%",
                    Confidence = CalculateAdaptationConfidence(currentMode, suggestedMode)
                };
            }
        }
        
        // Check for resource pressure adaptations
        var currentResourceUsage = _resourceManager.GetResourceUsage();
        if (currentResourceUsage.IsUnderMemoryPressure || currentResourceUsage.IsUnderCpuPressure)
        {
            var adaptation = CreateResourcePressureAdaptation(currentMode, currentResourceUsage);
            if (adaptation != null)
            {
                return adaptation;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Selects the optimal processing mode based on decision context
    /// </summary>
    private ProcessingMode SelectOptimalMode(ProcessingDecisionContext context)
    {
        // Rule-based decision making with performance history influence
        
        // Critical resource pressure - force sequential
        if (context.IsUnderMemoryPressure && context.MemoryUsagePercent > 90)
        {
            return ProcessingMode.Sequential;
        }
        
        if (context.IsUnderCpuPressure && context.CpuUsagePercent > 95)
        {
            return ProcessingMode.Sequential;
        }
        
        // Use performance history if available
        if (context.HasPerformanceHistory)
        {
            var historicalBest = GetBestPerformingMode(context);
            if (historicalBest != ProcessingMode.Sequential)
            {
                return historicalBest;
            }
        }
        
        // Default heuristic rules
        if (context.FileCount <= 3)
        {
            return ProcessingMode.Sequential;
        }
        
        if (context.SystemLoad > 0.8)
        {
            return ProcessingMode.Sequential;
        }
        
        if (context.FileCount <= 20 && context.SystemLoad < 0.6)
        {
            return ProcessingMode.Parallel;
        }
        
        if (context.FileCount > 50 && context.SystemLoad < 0.7)
        {
            return ProcessingMode.ProducerConsumer;
        }
        
        // Default to adaptive for complex scenarios
        return ProcessingMode.Adaptive;
    }
    
    /// <summary>
    /// Gets the best performing mode from performance history
    /// </summary>
    private ProcessingMode GetBestPerformingMode(ProcessingDecisionContext context)
    {
        // Find similar historical contexts
        var similarContexts = _performanceHistory
            .Where(r => IsSimilarContext(r.Data.Context, context))
            .GroupBy(r => r.Mode)
            .Select(g => new { Mode = g.Key, AverageEfficiency = g.Average(r => r.Efficiency) })
            .OrderByDescending(x => x.AverageEfficiency)
            .FirstOrDefault();
        
        return similarContexts?.Mode ?? ProcessingMode.Adaptive;
    }
    
    /// <summary>
    /// Determines if two processing contexts are similar
    /// </summary>
    private bool IsSimilarContext(ProcessingDecisionContext historical, ProcessingDecisionContext current)
    {
        // Simple similarity check based on key metrics
        var fileCountSimilar = Math.Abs(historical.FileCount - current.FileCount) < current.FileCount * 0.3;
        var memoryUsageSimilar = Math.Abs(historical.MemoryUsagePercent - current.MemoryUsagePercent) < 20;
        var systemLoadSimilar = Math.Abs(historical.SystemLoad - current.SystemLoad) < 0.3;
        
        return fileCountSimilar && memoryUsageSimilar && systemLoadSimilar;
    }
    
    /// <summary>
    /// Calculates processing efficiency based on performance data
    /// </summary>
    private double CalculateEfficiency(ProcessingPerformanceData data)
    {
        // Multi-factor efficiency calculation
        var factors = new List<double>();
        
        // Throughput factor (files per second)
        if (data.FilesPerSecond > 0)
        {
            var maxExpectedThroughput = Environment.ProcessorCount * 2; // Expected max files/sec
            factors.Add(Math.Min(100, (data.FilesPerSecond / maxExpectedThroughput) * 100));
        }
        
        // Memory efficiency factor
        if (data.PeakMemoryUsage > 0)
        {
            var memoryEfficiency = 100 - (data.PeakMemoryUsage / (2L * 1024 * 1024 * 1024) * 100); // 2GB baseline
            factors.Add(Math.Max(0, Math.Min(100, memoryEfficiency)));
        }
        
        // CPU efficiency factor (inverse of usage)
        if (data.AverageCpuUsage > 0)
        {
            var cpuEfficiency = 100 - Math.Min(100, data.AverageCpuUsage);
            factors.Add(Math.Max(0, cpuEfficiency));
        }
        
        // Error rate factor
        if (data.TotalFiles > 0)
        {
            var successRate = ((double)(data.TotalFiles - data.ErrorCount) / data.TotalFiles) * 100;
            factors.Add(successRate);
        }
        
        return factors.Count > 0 ? factors.Average() : 0;
    }
    
    /// <summary>
    /// Calculates average file size for processing decisions
    /// </summary>
    private long CalculateAverageFileSize(List<string> logFiles)
    {
        if (logFiles.Count == 0) return 0;
        
        var totalSize = 0L;
        var validFiles = 0;
        
        foreach (var file in logFiles.Take(Math.Min(10, logFiles.Count))) // Sample first 10 files
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Exists)
                {
                    totalSize += info.Length;
                    validFiles++;
                }
            }
            catch
            {
                // Ignore file access errors
            }
        }
        
        return validFiles > 0 ? totalSize / validFiles : 0;
    }
    
    /// <summary>
    /// Calculates overall system load factor
    /// </summary>
    private double CalculateSystemLoad(PerformanceMetrics performanceMetrics, ResourceUsageStats resourceUsage)
    {
        var factors = new List<double>();
        
        // CPU load factor
        factors.Add(performanceMetrics.CpuUsagePercent / 100.0);
        
        // Memory load factor
        factors.Add(resourceUsage.MemoryUsagePercent / 100.0);
        
        // Thread availability factor
        var threadUtilization = 1.0 - (double)resourceUsage.AvailableWorkerThreads / resourceUsage.MaxWorkerThreads;
        factors.Add(threadUtilization);
        
        return factors.Average();
    }
    
    /// <summary>
    /// Gets fallback processing mode based on file count
    /// </summary>
    private ProcessingMode GetFallbackMode(int fileCount)
    {
        return fileCount switch
        {
            <= 5 => ProcessingMode.Sequential,
            <= 25 => ProcessingMode.Parallel,
            _ => ProcessingMode.ProducerConsumer
        };
    }
    
    /// <summary>
    /// Creates adaptation recommendation for resource pressure
    /// </summary>
    private ProcessingAdaptation? CreateResourcePressureAdaptation(ProcessingMode currentMode, ResourceUsageStats resourceUsage)
    {
        if (resourceUsage.IsUnderMemoryPressure && currentMode != ProcessingMode.Sequential)
        {
            return new ProcessingAdaptation
            {
                SuggestedMode = ProcessingMode.Sequential,
                SuggestedWorkerCount = 1,
                SuggestedBatchSize = 10,
                Reason = "Memory pressure detected",
                Confidence = 0.9
            };
        }
        
        if (resourceUsage.IsUnderCpuPressure && currentMode == ProcessingMode.ProducerConsumer)
        {
            return new ProcessingAdaptation
            {
                SuggestedMode = ProcessingMode.Parallel,
                SuggestedWorkerCount = Math.Max(1, Environment.ProcessorCount / 2),
                SuggestedBatchSize = 50,
                Reason = "CPU pressure detected",
                Confidence = 0.8
            };
        }
        
        return null;
    }
    
    /// <summary>
    /// Calculates confidence level for adaptation recommendation
    /// </summary>
    private double CalculateAdaptationConfidence(ProcessingMode currentMode, ProcessingMode suggestedMode)
    {
        // Base confidence
        var confidence = 0.7;
        
        // Increase confidence based on mode statistics
        if (_modeStats.TryGetValue(suggestedMode, out var stats) && stats.TotalRuns > 5)
        {
            confidence += (stats.RecentAverageEfficiency - 50) / 100.0; // Scale recent performance
        }
        
        // Decrease confidence for dramatic mode changes
        if (currentMode == ProcessingMode.ProducerConsumer && suggestedMode == ProcessingMode.Sequential)
        {
            confidence -= 0.2;
        }
        
        return Math.Max(0.1, Math.Min(1.0, confidence));
    }
    
    /// <summary>
    /// Gets performance statistics for all processing modes
    /// </summary>
    public Dictionary<ProcessingMode, ProcessingModeStats> GetModeStatistics()
    {
        lock (_lock)
        {
            return new Dictionary<ProcessingMode, ProcessingModeStats>(_modeStats);
        }
    }
}

/// <summary>
/// Context information for processing mode decisions
/// </summary>
public class ProcessingDecisionContext
{
    public int FileCount { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double CpuUsagePercent { get; set; }
    public int AvailableWorkerThreads { get; set; }
    public bool IsUnderMemoryPressure { get; set; }
    public bool IsUnderCpuPressure { get; set; }
    public bool HasPerformanceHistory { get; set; }
    public long AverageFileSize { get; set; }
    public double SystemLoad { get; set; }
}

/// <summary>
/// Performance data for a processing operation
/// </summary>
public class ProcessingPerformanceData
{
    public TimeSpan ProcessingTime { get; set; }
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public int ErrorCount { get; set; }
    public long PeakMemoryUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public double FilesPerSecond => ProcessingTime.TotalSeconds > 0 ? FilesProcessed / ProcessingTime.TotalSeconds : 0;
    public ProcessingDecisionContext Context { get; set; } = new();
}

/// <summary>
/// Historical performance record
/// </summary>
public class ProcessingPerformanceRecord
{
    public DateTime Timestamp { get; set; }
    public ProcessingMode Mode { get; set; }
    public ProcessingPerformanceData Data { get; set; } = new();
    public double Efficiency { get; set; }
}

/// <summary>
/// Statistics for a specific processing mode
/// </summary>
public class ProcessingModeStats
{
    public ProcessingMode Mode { get; set; }
    public int TotalRuns { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public int TotalFilesProcessed { get; set; }
    public long TotalMemoryUsed { get; set; }
    public double EfficiencySum { get; set; }
    public double AverageEfficiency { get; set; }
    public double BestEfficiency { get; set; }
    public double WorstEfficiency { get; set; } = 100;
    public ProcessingDecisionContext? BestPerformanceContext { get; set; }
    public List<double> RecentEfficiencies { get; set; } = new();
    public double RecentAverageEfficiency { get; set; }
    public double AverageFilesPerSecond => TotalProcessingTime.TotalSeconds > 0 ? TotalFilesProcessed / TotalProcessingTime.TotalSeconds : 0;
}

/// <summary>
/// Adaptation recommendation
/// </summary>
public class ProcessingAdaptation
{
    public ProcessingMode SuggestedMode { get; set; }
    public int SuggestedWorkerCount { get; set; }
    public int SuggestedBatchSize { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}