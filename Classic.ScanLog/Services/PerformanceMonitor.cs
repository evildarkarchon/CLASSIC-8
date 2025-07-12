using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Logging;
using Classic.Core.Models;

namespace Classic.ScanLog.Services;

/// <summary>
/// Real-time performance monitoring service for tracking system resources and bottlenecks
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceMetrics _metrics;
    private readonly Timer _monitoringTimer;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private readonly object _lock = new();
    private bool _disposed = false;
    
    // System information
    private readonly Process _currentProcess;
    private readonly int _processorCount;
    private long _lastGC0, _lastGC1, _lastGC2;
    private long _lastBytesRead = 0;
    private DateTime _lastUpdateTime;
    
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _metrics = new PerformanceMetrics();
        _currentProcess = Process.GetCurrentProcess();
        _processorCount = Environment.ProcessorCount;
        _lastUpdateTime = DateTime.Now;
        
        // Initialize performance counters
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        
        // Initialize metrics
        _metrics.StartTime = DateTime.Now;
        _metrics.CpuCores = _processorCount;
        _metrics.OptimalWorkerThreads = CalculateOptimalWorkerCount();
        
        // Start monitoring timer (update every 2 seconds)
        _monitoringTimer = new Timer(UpdateMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        
        _logger.LogInformation("Performance monitor started with {CpuCores} CPU cores", _processorCount);
    }
    
    /// <summary>
    /// Updates all performance metrics
    /// </summary>
    private void UpdateMetrics(object? state)
    {
        if (_disposed) return;
        
        lock (_lock)
        {
            try
            {
                UpdateMemoryMetrics();
                UpdateCpuMetrics();
                UpdateThroughputMetrics();
                UpdateWorkerMetrics();
                UpdateGCMetrics();
                
                // Record snapshot and detect bottlenecks
                _metrics.RecordSnapshot();
                _metrics.DetectBottlenecks();
                
                // Log performance warnings if bottlenecks detected
                if (_metrics.DetectedBottlenecks.Count > 0)
                {
                    _logger.LogWarning("Performance bottlenecks detected: {Bottlenecks}", 
                        string.Join(", ", _metrics.DetectedBottlenecks));
                }
                
                _lastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metrics");
            }
        }
    }
    
    /// <summary>
    /// Updates memory-related metrics
    /// </summary>
    private void UpdateMemoryMetrics()
    {
        var workingSet = _currentProcess.WorkingSet64;
        var privateMemory = _currentProcess.PrivateMemorySize64;
        
        _metrics.CurrentMemoryUsage = workingSet;
        _metrics.TotalMemoryUsed = privateMemory;
        
        if (workingSet > _metrics.PeakMemoryUsage)
        {
            _metrics.PeakMemoryUsage = workingSet;
        }
        
        // Get available system memory
        var availableMemory = _memoryCounter.NextValue() * 1024 * 1024; // Convert MB to bytes
        _logger.LogTrace("Memory: Working={WorkingMB}MB, Private={PrivateMB}MB, Available={AvailableMB}MB", 
            workingSet / 1024 / 1024, privateMemory / 1024 / 1024, availableMemory / 1024 / 1024);
    }
    
    /// <summary>
    /// Updates CPU usage metrics
    /// </summary>
    private void UpdateCpuMetrics()
    {
        try
        {
            var totalProcessorTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var currentTime = DateTime.Now;
            var elapsedTime = (currentTime - _lastUpdateTime).TotalMilliseconds;
            
            if (elapsedTime > 0)
            {
                // Calculate CPU usage for this process
                var cpuUsage = (totalProcessorTime / elapsedTime) * 100 / _processorCount;
                _metrics.CpuUsagePercent = Math.Min(100, Math.Max(0, cpuUsage));
                
                // Update peak and average
                if (_metrics.CpuUsagePercent > _metrics.PeakCpuUsage)
                {
                    _metrics.PeakCpuUsage = _metrics.CpuUsagePercent;
                }
                
                // Simple moving average for CPU usage
                _metrics.AverageCpuUsage = (_metrics.AverageCpuUsage * 0.7) + (_metrics.CpuUsagePercent * 0.3);
            }
            
            _logger.LogTrace("CPU: Current={CpuUsage:F1}%, Peak={PeakCpu:F1}%, Average={AvgCpu:F1}%", 
                _metrics.CpuUsagePercent, _metrics.PeakCpuUsage, _metrics.AverageCpuUsage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update CPU metrics");
        }
    }
    
    /// <summary>
    /// Updates throughput and I/O metrics
    /// </summary>
    private void UpdateThroughputMetrics()
    {
        var currentTime = DateTime.Now;
        var elapsedSeconds = (currentTime - _lastUpdateTime).TotalSeconds;
        
        if (elapsedSeconds > 0)
        {
            // Calculate current throughput
            var filesDelta = _metrics.TotalFilesProcessed - (_metrics.Snapshots.LastOrDefault()?.FilesProcessed ?? 0);
            _metrics.CurrentThroughput = (int)(filesDelta / elapsedSeconds);
            
            if (_metrics.CurrentThroughput > _metrics.PeakThroughput)
            {
                _metrics.PeakThroughput = _metrics.CurrentThroughput;
            }
            
            // Calculate bytes per second
            var bytesDelta = _metrics.TotalBytesRead - _lastBytesRead;
            _metrics.MegabytesPerSecond = (bytesDelta / elapsedSeconds) / 1024 / 1024;
            _lastBytesRead = _metrics.TotalBytesRead;
            
            _logger.LogTrace("Throughput: {CurrentThroughput} files/sec, {MBPerSec:F2} MB/sec", 
                _metrics.CurrentThroughput, _metrics.MegabytesPerSecond);
        }
    }
    
    /// <summary>
    /// Updates worker thread metrics
    /// </summary>
    private void UpdateWorkerMetrics()
    {
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        
        var activeWorkerThreads = maxWorkerThreads - availableWorkerThreads;
        _metrics.WorkerThreadsUsed = activeWorkerThreads;
        _metrics.IdleWorkerThreads = availableWorkerThreads;
        
        // Calculate worker efficiency
        if (_metrics.OptimalWorkerThreads > 0)
        {
            _metrics.WorkerEfficiency = Math.Min(100, (double)activeWorkerThreads / _metrics.OptimalWorkerThreads * 100);
        }
        
        _logger.LogTrace("Workers: Active={Active}, Available={Available}, Optimal={Optimal}, Efficiency={Efficiency:F1}%", 
            activeWorkerThreads, availableWorkerThreads, _metrics.OptimalWorkerThreads, _metrics.WorkerEfficiency);
    }
    
    /// <summary>
    /// Updates garbage collection metrics
    /// </summary>
    private void UpdateGCMetrics()
    {
        var gc0 = GC.CollectionCount(0);
        var gc1 = GC.CollectionCount(1);
        var gc2 = GC.CollectionCount(2);
        
        var totalGC = gc0 + gc1 + gc2;
        var lastTotalGC = _lastGC0 + _lastGC1 + _lastGC2;
        
        if (totalGC > lastTotalGC)
        {
            _metrics.GCCollections = totalGC;
            
            // Estimate memory released (rough approximation)
            var memoryBefore = _metrics.Snapshots.LastOrDefault()?.MemoryUsage ?? _metrics.CurrentMemoryUsage;
            var memoryAfter = _metrics.CurrentMemoryUsage;
            if (memoryBefore > memoryAfter)
            {
                _metrics.GCMemoryReleased += memoryBefore - memoryAfter;
            }
            
            _logger.LogTrace("GC: Total={Total}, Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}", 
                totalGC, gc0, gc1, gc2);
        }
        
        _lastGC0 = gc0;
        _lastGC1 = gc1;
        _lastGC2 = gc2;
    }
    
    /// <summary>
    /// Records timing for a specific component
    /// </summary>
    public void RecordComponentTime(string component, TimeSpan duration)
    {
        lock (_lock)
        {
            _metrics.ComponentTimes.TryGetValue(component, out var existingTime);
            _metrics.ComponentTimes[component] = existingTime + duration;
            
            _metrics.ComponentCounts.TryGetValue(component, out var count);
            _metrics.ComponentCounts[component] = count + 1;
        }
    }
    
    /// <summary>
    /// Records memory usage for a specific component
    /// </summary>
    public void RecordComponentMemory(string component, long memoryUsage)
    {
        lock (_lock)
        {
            _metrics.ComponentMemoryUsage.TryGetValue(component, out var existingMemory);
            _metrics.ComponentMemoryUsage[component] = Math.Max(existingMemory, memoryUsage);
        }
    }
    
    /// <summary>
    /// Updates file I/O metrics
    /// </summary>
    public void RecordFileOperation(TimeSpan duration, long bytesRead = 0)
    {
        lock (_lock)
        {
            _metrics.FileSystemOperations++;
            _metrics.TotalFileIOTime += duration;
            _metrics.TotalBytesRead += bytesRead;
        }
    }
    
    /// <summary>
    /// Updates database operation metrics
    /// </summary>
    public void RecordDatabaseOperation(TimeSpan duration)
    {
        lock (_lock)
        {
            _metrics.DatabaseQueries++;
            _metrics.TotalDatabaseTime += duration;
        }
    }
    
    /// <summary>
    /// Updates cache metrics
    /// </summary>
    public void RecordCacheOperation(bool hit, long memoryUsage = 0)
    {
        lock (_lock)
        {
            if (hit)
                _metrics.CacheHits++;
            else
                _metrics.CacheMisses++;
            
            if (memoryUsage > 0)
                _metrics.CacheMemoryUsage = Math.Max(_metrics.CacheMemoryUsage, memoryUsage);
        }
    }
    
    /// <summary>
    /// Calculates optimal worker thread count based on system resources
    /// </summary>
    private int CalculateOptimalWorkerCount()
    {
        // Base on CPU cores, but consider I/O intensive nature of crash log processing
        var baseCores = _processorCount;
        
        // For I/O intensive workloads, we can use more threads than CPU cores
        // Rule of thumb: 1.5-2x cores for I/O bound work
        var optimalCount = (int)(baseCores * 1.5);
        
        // Cap at reasonable maximum to prevent resource exhaustion
        return Math.Min(optimalCount, 32);
    }
    
    /// <summary>
    /// Gets a summary of current performance status
    /// </summary>
    public string GetPerformanceSummary()
    {
        lock (_lock)
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Performance Summary ({DateTime.Now:HH:mm:ss}):");
            summary.AppendLine($"  CPU: {_metrics.CpuUsagePercent:F1}% (Peak: {_metrics.PeakCpuUsage:F1}%)");
            summary.AppendLine($"  Memory: {_metrics.CurrentMemoryUsage / 1024 / 1024:F0} MB (Peak: {_metrics.PeakMemoryUsage / 1024 / 1024:F0} MB)");
            summary.AppendLine($"  Throughput: {_metrics.CurrentThroughput} files/sec (Peak: {_metrics.PeakThroughput})");
            summary.AppendLine($"  Workers: {_metrics.WorkerThreadsUsed} active, {_metrics.WorkerEfficiency:F1}% efficiency");
            summary.AppendLine($"  Cache: {_metrics.CacheHitRate:F1}% hit rate");
            summary.AppendLine($"  Overall Efficiency: {_metrics.OverallEfficiency:F1}%");
            
            if (_metrics.DetectedBottlenecks.Count > 0)
            {
                summary.AppendLine($"  Bottlenecks: {string.Join(", ", _metrics.DetectedBottlenecks)}");
            }
            
            return summary.ToString();
        }
    }
    
    /// <summary>
    /// Suggests optimal processing mode based on current performance
    /// </summary>
    public ProcessingMode SuggestOptimalMode(int totalFiles)
    {
        lock (_lock)
        {
            // Consider multiple factors
            var cpuUtilization = _metrics.AverageCpuUsage;
            var memoryPressure = _metrics.CurrentMemoryUsage / (double)_metrics.PeakMemoryUsage;
            var workerEfficiency = _metrics.WorkerEfficiency;
            
            // Simple heuristic for mode selection
            if (totalFiles <= 5)
            {
                return ProcessingMode.Sequential;
            }
            
            if (cpuUtilization > 80 || memoryPressure > 0.8)
            {
                // System under pressure, use sequential
                return ProcessingMode.Sequential;
            }
            
            if (totalFiles <= 20 && workerEfficiency > 70)
            {
                // Good for parallel processing
                return ProcessingMode.Parallel;
            }
            
            if (totalFiles > 50 && workerEfficiency > 60)
            {
                // Large batch, use producer-consumer
                return ProcessingMode.ProducerConsumer;
            }
            
            // Default to adaptive for complex scenarios
            return ProcessingMode.Adaptive;
        }
    }
    
    /// <summary>
    /// Gets the current performance metrics
    /// </summary>
    public async Task<PerformanceMetrics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_metrics);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _currentProcess?.Dispose();
            _disposed = true;
        }
    }
}