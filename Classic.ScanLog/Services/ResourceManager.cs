using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Logging;
using Classic.Core.Models;

namespace Classic.ScanLog.Services;

/// <summary>
/// Manages system resources and provides automatic optimization
/// </summary>
public class ResourceManager : IDisposable
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly Timer _optimizationTimer;
    private readonly Process _currentProcess;
    private readonly object _lock = new();
    private bool _disposed = false;
    
    // Configuration
    private readonly long _memoryPressureThreshold;
    private readonly double _cpuPressureThreshold;
    private readonly TimeSpan _optimizationInterval;
    
    // Resource tracking
    private DateTime _lastGCOptimization = DateTime.MinValue;
    private DateTime _lastMemoryCleanup = DateTime.MinValue;
    private int _consecutiveHighMemoryWarnings = 0;
    private int _consecutiveHighCpuWarnings = 0;
    
    // Resource limits
    private readonly long _maxMemoryUsage;
    private readonly int _maxWorkerThreads;
    
    public ResourceManager(
        ILogger<ResourceManager> logger,
        PerformanceMonitor performanceMonitor,
        long memoryPressureThreshold = 1024 * 1024 * 1024, // 1GB
        double cpuPressureThreshold = 80.0, // 80% CPU
        TimeSpan? optimizationInterval = null)
    {
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _memoryPressureThreshold = memoryPressureThreshold;
        _cpuPressureThreshold = cpuPressureThreshold;
        _optimizationInterval = optimizationInterval ?? TimeSpan.FromSeconds(30);
        
        _currentProcess = Process.GetCurrentProcess();
        
        // Calculate resource limits based on system capacity
        var totalMemory = GC.GetTotalMemory(false);
        var availableMemory = GetAvailablePhysicalMemory();
        _maxMemoryUsage = Math.Min(availableMemory / 2, 4L * 1024 * 1024 * 1024); // Max 4GB or half available
        
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
        _maxWorkerThreads = Math.Min(maxWorkerThreads, Environment.ProcessorCount * 4);
        
        // Start optimization timer
        _optimizationTimer = new Timer(OptimizeResources, null, _optimizationInterval, _optimizationInterval);
        
        _logger.LogInformation("Resource manager initialized - Memory limit: {MemoryLimitMB}MB, Max workers: {MaxWorkers}", 
            _maxMemoryUsage / 1024 / 1024, _maxWorkerThreads);
    }
    
    /// <summary>
    /// Optimizes system resources based on current usage
    /// </summary>
    private void OptimizeResources(object? state)
    {
        if (_disposed) return;
        
        lock (_lock)
        {
            try
            {
                var currentMemory = _currentProcess.WorkingSet64;
                var currentCpu = _performanceMonitor.GetStatisticsAsync().Result.CpuUsagePercent;
                
                // Check memory pressure
                if (currentMemory > _memoryPressureThreshold)
                {
                    _consecutiveHighMemoryWarnings++;
                    HandleMemoryPressure(currentMemory);
                }
                else
                {
                    _consecutiveHighMemoryWarnings = 0;
                }
                
                // Check CPU pressure
                if (currentCpu > _cpuPressureThreshold)
                {
                    _consecutiveHighCpuWarnings++;
                    HandleCpuPressure(currentCpu);
                }
                else
                {
                    _consecutiveHighCpuWarnings = 0;
                }
                
                // Proactive optimization
                if (ShouldPerformProactiveOptimization())
                {
                    PerformProactiveOptimization();
                }
                
                // Log resource status
                LogResourceStatus(currentMemory, currentCpu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource optimization");
            }
        }
    }
    
    /// <summary>
    /// Handles memory pressure situations
    /// </summary>
    private void HandleMemoryPressure(long currentMemory)
    {
        _logger.LogWarning("Memory pressure detected: {CurrentMemoryMB}MB (threshold: {ThresholdMB}MB)", 
            currentMemory / 1024 / 1024, _memoryPressureThreshold / 1024 / 1024);
        
        // Immediate actions for memory pressure
        if (_consecutiveHighMemoryWarnings >= 3)
        {
            _logger.LogWarning("Severe memory pressure - performing aggressive cleanup");
            
            // Force garbage collection
            ForceGarbageCollection();
            
            // Clear caches if available
            ClearCaches();
            
            // Reduce worker threads temporarily
            ReduceWorkerThreads();
        }
        else if (_consecutiveHighMemoryWarnings >= 2)
        {
            // Moderate memory pressure
            _logger.LogInformation("Moderate memory pressure - performing standard cleanup");
            
            // Standard garbage collection
            PerformOptimizedGarbageCollection();
            
            // Reduce batch sizes
            SuggestBatchSizeReduction();
        }
    }
    
    /// <summary>
    /// Handles CPU pressure situations
    /// </summary>
    private void HandleCpuPressure(double currentCpu)
    {
        _logger.LogWarning("CPU pressure detected: {CurrentCpu:F1}% (threshold: {ThresholdCpu:F1}%)", 
            currentCpu, _cpuPressureThreshold);
        
        if (_consecutiveHighCpuWarnings >= 3)
        {
            _logger.LogWarning("Severe CPU pressure - reducing processing intensity");
            
            // Reduce worker threads
            ReduceWorkerThreads();
            
            // Suggest sequential processing
            SuggestSequentialProcessing();
        }
        else if (_consecutiveHighCpuWarnings >= 2)
        {
            _logger.LogInformation("Moderate CPU pressure - optimizing thread usage");
            
            // Optimize thread pool
            OptimizeThreadPool();
        }
    }
    
    /// <summary>
    /// Performs proactive optimization when conditions are favorable
    /// </summary>
    private void PerformProactiveOptimization()
    {
        var timeSinceLastOptimization = DateTime.UtcNow - _lastGCOptimization;
        
        if (timeSinceLastOptimization > TimeSpan.FromMinutes(5))
        {
            _logger.LogDebug("Performing proactive resource optimization");
            
            // Gentle garbage collection
            PerformOptimizedGarbageCollection();
            
            // Optimize thread pool settings
            OptimizeThreadPool();
            
            // Compact large object heap if beneficial
            if (ShouldCompactLargeObjectHeap())
            {
                CompactLargeObjectHeap();
            }
            
            _lastGCOptimization = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Forces aggressive garbage collection
    /// </summary>
    private void ForceGarbageCollection()
    {
        var beforeMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false);
        var memoryReleased = beforeMemory - afterMemory;
        
        _logger.LogInformation("Force GC completed in {ElapsedMs}ms, released {ReleasedMB}MB", 
            stopwatch.ElapsedMilliseconds, memoryReleased / 1024 / 1024);
    }
    
    /// <summary>
    /// Performs optimized garbage collection
    /// </summary>
    private void PerformOptimizedGarbageCollection()
    {
        var beforeMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();
        
        // Collect only if beneficial
        if (GC.GetTotalMemory(false) > _memoryPressureThreshold / 2)
        {
            GC.Collect(1); // Collect generations 0 and 1
            
            var afterMemory = GC.GetTotalMemory(false);
            var memoryReleased = beforeMemory - afterMemory;
            
            _logger.LogDebug("Optimized GC completed in {ElapsedMs}ms, released {ReleasedMB}MB", 
                stopwatch.ElapsedMilliseconds, memoryReleased / 1024 / 1024);
        }
    }
    
    /// <summary>
    /// Clears various caches to free memory
    /// </summary>
    private void ClearCaches()
    {
        // This would integrate with cache systems in the application
        _logger.LogDebug("Clearing application caches");
        
        // Example: Clear FormID cache, report cache, etc.
        // Implementation depends on the specific cache systems in use
    }
    
    /// <summary>
    /// Temporarily reduces worker thread count
    /// </summary>
    private void ReduceWorkerThreads()
    {
        ThreadPool.GetMaxThreads(out var currentMaxWorkerThreads, out var currentMaxCompletionPortThreads);
        var newMaxWorkerThreads = Math.Max(Environment.ProcessorCount, currentMaxWorkerThreads / 2);
        
        ThreadPool.SetMaxThreads(newMaxWorkerThreads, currentMaxCompletionPortThreads);
        
        _logger.LogInformation("Reduced max worker threads from {OldMax} to {NewMax}", 
            currentMaxWorkerThreads, newMaxWorkerThreads);
    }
    
    /// <summary>
    /// Optimizes thread pool settings
    /// </summary>
    private void OptimizeThreadPool()
    {
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
        
        var optimalMinWorkerThreads = Math.Min(Environment.ProcessorCount, maxWorkerThreads / 4);
        var optimalMaxWorkerThreads = Math.Min(_maxWorkerThreads, Environment.ProcessorCount * 2);
        
        if (minWorkerThreads != optimalMinWorkerThreads || maxWorkerThreads != optimalMaxWorkerThreads)
        {
            ThreadPool.SetMinThreads(optimalMinWorkerThreads, minCompletionPortThreads);
            ThreadPool.SetMaxThreads(optimalMaxWorkerThreads, maxCompletionPortThreads);
            
            _logger.LogDebug("Optimized thread pool: Min={MinWorkers}, Max={MaxWorkers}", 
                optimalMinWorkerThreads, optimalMaxWorkerThreads);
        }
    }
    
    /// <summary>
    /// Suggests batch size reduction to calling code
    /// </summary>
    private void SuggestBatchSizeReduction()
    {
        _logger.LogInformation("Suggesting batch size reduction due to memory pressure");
        // This would trigger an event or callback to reduce batch sizes
    }
    
    /// <summary>
    /// Suggests sequential processing mode
    /// </summary>
    private void SuggestSequentialProcessing()
    {
        _logger.LogInformation("Suggesting sequential processing mode due to CPU pressure");
        // This would trigger an event or callback to switch processing modes
    }
    
    /// <summary>
    /// Determines if proactive optimization should be performed
    /// </summary>
    private bool ShouldPerformProactiveOptimization()
    {
        var currentMemory = _currentProcess.WorkingSet64;
        var memoryUsagePercent = (double)currentMemory / _maxMemoryUsage * 100;
        
        // Perform proactive optimization if memory usage is moderate but not critical
        return memoryUsagePercent > 50 && memoryUsagePercent < 80 && 
               _consecutiveHighMemoryWarnings == 0 && 
               _consecutiveHighCpuWarnings == 0;
    }
    
    /// <summary>
    /// Determines if large object heap should be compacted
    /// </summary>
    private bool ShouldCompactLargeObjectHeap()
    {
        // Compact LOH if it's been more than 10 minutes since last compaction
        var timeSinceLastCleanup = DateTime.UtcNow - _lastMemoryCleanup;
        return timeSinceLastCleanup > TimeSpan.FromMinutes(10);
    }
    
    /// <summary>
    /// Compacts the large object heap
    /// </summary>
    private void CompactLargeObjectHeap()
    {
        try
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            
            _lastMemoryCleanup = DateTime.UtcNow;
            _logger.LogDebug("Large object heap compaction completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compact large object heap");
        }
    }
    
    /// <summary>
    /// Gets available physical memory
    /// </summary>
    private long GetAvailablePhysicalMemory()
    {
        try
        {
            var memoryInfo = GC.GetGCMemoryInfo();
            return memoryInfo.TotalAvailableMemoryBytes;
        }
        catch
        {
            // Fallback to a reasonable default
            return 8L * 1024 * 1024 * 1024; // 8GB
        }
    }
    
    /// <summary>
    /// Logs current resource status
    /// </summary>
    private void LogResourceStatus(long currentMemory, double currentCpu)
    {
        var memoryUsagePercent = (double)currentMemory / _maxMemoryUsage * 100;
        
        _logger.LogTrace("Resource Status - Memory: {MemoryMB}MB ({MemoryPercent:F1}%), CPU: {CpuPercent:F1}%", 
            currentMemory / 1024 / 1024, memoryUsagePercent, currentCpu);
    }
    
    /// <summary>
    /// Gets current resource usage statistics
    /// </summary>
    public ResourceUsageStats GetResourceUsage()
    {
        var currentMemory = _currentProcess.WorkingSet64;
        var totalMemory = GC.GetTotalMemory(false);
        var memoryUsagePercent = (double)currentMemory / _maxMemoryUsage * 100;
        
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        
        return new ResourceUsageStats
        {
            CurrentMemoryUsage = currentMemory,
            TotalManagedMemory = totalMemory,
            MemoryUsagePercent = memoryUsagePercent,
            AvailableWorkerThreads = availableWorkerThreads,
            MaxWorkerThreads = maxWorkerThreads,
            IsUnderMemoryPressure = currentMemory > _memoryPressureThreshold,
            IsUnderCpuPressure = _consecutiveHighCpuWarnings > 0,
            ConsecutiveMemoryWarnings = _consecutiveHighMemoryWarnings,
            ConsecutiveCpuWarnings = _consecutiveHighCpuWarnings
        };
    }
    
    /// <summary>
    /// Suggests optimal processing settings based on current resources
    /// </summary>
    public ProcessingRecommendations GetProcessingRecommendations()
    {
        var usage = GetResourceUsage();
        var recommendations = new ProcessingRecommendations();
        
        // Memory-based recommendations
        if (usage.MemoryUsagePercent > 80)
        {
            recommendations.SuggestedProcessingMode = ProcessingMode.Sequential;
            recommendations.SuggestedBatchSize = 10;
            recommendations.SuggestedMaxWorkers = Environment.ProcessorCount / 2;
        }
        else if (usage.MemoryUsagePercent > 60)
        {
            recommendations.SuggestedProcessingMode = ProcessingMode.Parallel;
            recommendations.SuggestedBatchSize = 50;
            recommendations.SuggestedMaxWorkers = Environment.ProcessorCount;
        }
        else
        {
            recommendations.SuggestedProcessingMode = ProcessingMode.ProducerConsumer;
            recommendations.SuggestedBatchSize = 100;
            recommendations.SuggestedMaxWorkers = Environment.ProcessorCount * 2;
        }
        
        // CPU-based adjustments
        if (usage.IsUnderCpuPressure)
        {
            recommendations.SuggestedMaxWorkers = Math.Max(1, recommendations.SuggestedMaxWorkers / 2);
        }
        
        recommendations.RecommendGarbageCollection = usage.MemoryUsagePercent > 70;
        recommendations.RecommendCacheClear = usage.MemoryUsagePercent > 85;
        
        return recommendations;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _optimizationTimer?.Dispose();
            _currentProcess?.Dispose();
            _disposed = true;
            
            _logger.LogInformation("Resource manager disposed");
        }
    }
}

/// <summary>
/// Resource usage statistics
/// </summary>
public class ResourceUsageStats
{
    public long CurrentMemoryUsage { get; set; }
    public long TotalManagedMemory { get; set; }
    public double MemoryUsagePercent { get; set; }
    public int AvailableWorkerThreads { get; set; }
    public int MaxWorkerThreads { get; set; }
    public bool IsUnderMemoryPressure { get; set; }
    public bool IsUnderCpuPressure { get; set; }
    public int ConsecutiveMemoryWarnings { get; set; }
    public int ConsecutiveCpuWarnings { get; set; }
}

/// <summary>
/// Processing recommendations based on current resource usage
/// </summary>
public class ProcessingRecommendations
{
    public ProcessingMode SuggestedProcessingMode { get; set; }
    public int SuggestedBatchSize { get; set; }
    public int SuggestedMaxWorkers { get; set; }
    public bool RecommendGarbageCollection { get; set; }
    public bool RecommendCacheClear { get; set; }
}