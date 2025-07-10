using System.Collections.Concurrent;
using Classic.Core.Models;

namespace Classic.ScanLog.Models;

/// <summary>
///     Thread-safe cache for crash log data and analysis results.
///     Equivalent to Python's ThreadSafeLogCache.
/// </summary>
public class ConcurrentLogCache
{
    private readonly TimeSpan _cacheTimeout;
    private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly object _cleanupLock = new();
    private readonly ConcurrentDictionary<string, CrashLog> _crashLogCache;
    private readonly ConcurrentDictionary<string, ScanResult> _scanResultCache;
    private DateTime _lastCleanup = DateTime.UtcNow;

    public ConcurrentLogCache(TimeSpan cacheTimeout = default)
    {
        _cacheTimeout = cacheTimeout == default ? TimeSpan.FromMinutes(5) : cacheTimeout;
        _crashLogCache = new ConcurrentDictionary<string, CrashLog>();
        _scanResultCache = new ConcurrentDictionary<string, ScanResult>();
        _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    ///     Adds or updates a crash log in the cache
    /// </summary>
    public void SetCrashLog(string key, CrashLog crashLog)
    {
        _crashLogCache.AddOrUpdate(key, crashLog, (k, v) => crashLog);
        _cacheTimestamps.AddOrUpdate(key, DateTime.UtcNow, (k, v) => DateTime.UtcNow);
        TryCleanupExpiredEntries();
    }

    /// <summary>
    ///     Retrieves a crash log from the cache
    /// </summary>
    public CrashLog? GetCrashLog(string key)
    {
        if (!_crashLogCache.TryGetValue(key, out var crashLog))
            return null;

        if (IsExpired(key))
        {
            RemoveCrashLog(key);
            return null;
        }

        return crashLog;
    }

    /// <summary>
    ///     Adds or updates a scan result in the cache
    /// </summary>
    public void SetScanResult(string key, ScanResult scanResult)
    {
        _scanResultCache.AddOrUpdate(key, scanResult, (k, v) => scanResult);
        _cacheTimestamps.AddOrUpdate(key + "_result", DateTime.UtcNow, (k, v) => DateTime.UtcNow);
        TryCleanupExpiredEntries();
    }

    /// <summary>
    ///     Retrieves a scan result from the cache
    /// </summary>
    public ScanResult? GetScanResult(string key)
    {
        if (!_scanResultCache.TryGetValue(key, out var scanResult))
            return null;

        if (IsExpired(key + "_result"))
        {
            RemoveScanResult(key);
            return null;
        }

        return scanResult;
    }

    /// <summary>
    ///     Removes a crash log from the cache
    /// </summary>
    public bool RemoveCrashLog(string key)
    {
        var removed = _crashLogCache.TryRemove(key, out _);
        _cacheTimestamps.TryRemove(key, out _);
        return removed;
    }

    /// <summary>
    ///     Removes a scan result from the cache
    /// </summary>
    public bool RemoveScanResult(string key)
    {
        var removed = _scanResultCache.TryRemove(key, out _);
        _cacheTimestamps.TryRemove(key + "_result", out _);
        return removed;
    }

    /// <summary>
    ///     Clears all cached data
    /// </summary>
    public void Clear()
    {
        _crashLogCache.Clear();
        _scanResultCache.Clear();
        _cacheTimestamps.Clear();
    }

    /// <summary>
    ///     Gets the current cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            CrashLogCount = _crashLogCache.Count,
            ScanResultCount = _scanResultCache.Count,
            TotalEntries = _crashLogCache.Count + _scanResultCache.Count,
            LastCleanup = _lastCleanup,
            CacheTimeout = _cacheTimeout
        };
    }

    /// <summary>
    ///     Checks if a cache entry has expired
    /// </summary>
    private bool IsExpired(string key)
    {
        if (!_cacheTimestamps.TryGetValue(key, out var timestamp))
            return true;

        return DateTime.UtcNow - timestamp > _cacheTimeout;
    }

    /// <summary>
    ///     Performs cleanup of expired entries if enough time has passed
    /// </summary>
    private void TryCleanupExpiredEntries()
    {
        if (DateTime.UtcNow - _lastCleanup < _cleanupInterval)
            return;

        lock (_cleanupLock)
        {
            if (DateTime.UtcNow - _lastCleanup < _cleanupInterval)
                return;

            CleanupExpiredEntries();
            _lastCleanup = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Removes all expired entries from the cache
    /// </summary>
    private void CleanupExpiredEntries()
    {
        var expiredKeys = new List<string>();

        foreach (var kvp in _cacheTimestamps)
            if (DateTime.UtcNow - kvp.Value > _cacheTimeout)
                expiredKeys.Add(kvp.Key);

        foreach (var key in expiredKeys)
        {
            _cacheTimestamps.TryRemove(key, out _);

            if (key.EndsWith("_result"))
            {
                var originalKey = key.Substring(0, key.Length - "_result".Length);
                _scanResultCache.TryRemove(originalKey, out _);
            }
            else
            {
                _crashLogCache.TryRemove(key, out _);
            }
        }
    }
}

/// <summary>
///     Cache statistics information
/// </summary>
public class CacheStatistics
{
    public int CrashLogCount { get; set; }
    public int ScanResultCount { get; set; }
    public int TotalEntries { get; set; }
    public DateTime LastCleanup { get; set; }
    public TimeSpan CacheTimeout { get; set; }
}

/// <summary>
///     Scan result model containing analysis data
/// </summary>
public class ScanResult
{
    public string LogFileName { get; set; } = string.Empty;
    public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan ScanDuration { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    // Analysis results
    public List<DetectedSuspect> DetectedSuspects { get; set; } = new();
    public PluginAnalysisResult PluginAnalysis { get; set; } = new();
    public SystemAnalysisResult SystemAnalysis { get; set; } = new();
    public List<RecommendedAction> Recommendations { get; set; } = new();

    // Performance metrics
    public ScanPerformanceMetrics PerformanceMetrics { get; set; } = new();

    // Raw data for further analysis
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class DetectedSuspect
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; }
    public List<string> MatchedPatterns { get; set; } = new();
    public List<string> Solutions { get; set; } = new();
    public string DocumentationUrl { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
}

public class PluginAnalysisResult
{
    public int TotalPlugins { get; set; }
    public int LightPlugins { get; set; }
    public int RegularPlugins { get; set; }
    public bool HasPluginList { get; set; }
    public List<string> ProblematicPlugins { get; set; } = new();
    public List<string> ConflictingPlugins { get; set; } = new();
    public List<string> MissingPatches { get; set; } = new();
    public bool ExceedsRecommendedLimit { get; set; }
}

public class SystemAnalysisResult
{
    public string OperatingSystem { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public List<string> GpUs { get; set; } = new();
    public string MemoryInfo { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public string BuffoutVersion { get; set; } = string.Empty;
    public bool IsLatestBuffout { get; set; }
    public Dictionary<string, bool> BuffoutSettings { get; set; } = new();
}

public class RecommendedAction
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public string MoreInfoUrl { get; set; } = string.Empty;
}

public class ScanPerformanceMetrics
{
    public TimeSpan ParseTime { get; set; }
    public TimeSpan AnalysisTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public long MemoryUsed { get; set; }
    public ProcessingStrategy UsedStrategy { get; set; }
    public int ThreadsUsed { get; set; }
    public Dictionary<string, TimeSpan> ComponentTimes { get; set; } = new();
}
