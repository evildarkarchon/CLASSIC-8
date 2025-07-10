using System.Diagnostics;
using System.Threading.Channels;
using Classic.Core.Interfaces;
using Classic.ScanLog.Analyzers;
using Classic.ScanLog.Models;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Orchestration;

/// <summary>
///     Orchestrates the crash log scanning process with adaptive processing strategies.
///     Implements the IScanOrchestrator interface.
/// </summary>
public class AdaptiveScanOrchestrator : IScanOrchestrator
{
    private readonly ConcurrentLogCache _cache;
    private readonly ScanLogConfiguration _configuration;
    private readonly ICrashLogParser _crashLogParser;

    private readonly ProcessingStrategy _currentStrategy = ProcessingStrategy.Sequential;
    private readonly IFormIdAnalyzer _formIdAnalyzer;
    private readonly ILogger<AdaptiveScanOrchestrator> _logger;
    private readonly IMessageHandler _messageHandler;
    private readonly IPluginAnalyzer _pluginAnalyzer;
    private readonly SemaphoreSlim _strategySemaphore = new(1, 1);
    private readonly SuspectScanner _suspectScanner;
    private DateTime _lastStrategyEvaluation = DateTime.UtcNow;

    public AdaptiveScanOrchestrator(
        ILogger<AdaptiveScanOrchestrator> logger,
        ICrashLogParser crashLogParser,
        IPluginAnalyzer pluginAnalyzer,
        IFormIdAnalyzer formIdAnalyzer,
        SuspectScanner suspectScanner,
        ConcurrentLogCache cache,
        ScanLogConfiguration configuration,
        IMessageHandler messageHandler)
    {
        _logger = logger;
        _crashLogParser = crashLogParser;
        _pluginAnalyzer = pluginAnalyzer;
        _formIdAnalyzer = formIdAnalyzer;
        _suspectScanner = suspectScanner;
        _cache = cache;
        _configuration = configuration;
        _messageHandler = messageHandler;
    }

    /// <summary>
    ///     Executes a comprehensive scan of crash logs
    /// </summary>
    public async Task<object> ExecuteScanAsync(object request, CancellationToken cancellationToken = default)
    {
        if (request is not ScanRequest scanRequest)
            throw new ArgumentException("Request must be a ScanRequest object", nameof(request));

        return await ExecuteScanInternalAsync(scanRequest, cancellationToken);
    }

    /// <summary>
    ///     Executes a single crash log scan
    /// </summary>
    public async Task<object> ScanSingleLogAsync(string logPath, CancellationToken cancellationToken = default)
    {
        return await ScanSingleLogInternalAsync(logPath, cancellationToken);
    }

    /// <summary>
    ///     Gets performance statistics for the orchestrator
    /// </summary>
    public async Task<object> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetStatisticsInternalAsync(cancellationToken);
    }

    /// <summary>
    ///     Internal implementation that works with strongly typed parameters
    /// </summary>
    public async Task<ScanResult> ExecuteScanInternalAsync(ScanRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting scan execution for: {LogPaths}", string.Join(", ", request.LogPaths));

            await _messageHandler.SendMessageAsync("Starting crash log analysis...", cancellationToken);

            // Select optimal processing strategy
            var strategy = await SelectOptimalStrategyAsync(request, cancellationToken);

            ScanResult result;

            switch (strategy)
            {
                case ProcessingStrategy.Sequential:
                    result = await ExecuteSequentialScanAsync(request, cancellationToken);
                    break;
                case ProcessingStrategy.Parallel:
                    result = await ExecuteParallelScanAsync(request, cancellationToken);
                    break;
                case ProcessingStrategy.ProducerConsumer:
                    result = await ExecuteProducerConsumerScanAsync(request, cancellationToken);
                    break;
                default:
                    result = await ExecuteSequentialScanAsync(request, cancellationToken);
                    break;
            }

            result.PerformanceMetrics.TotalProcessingTime = stopwatch.Elapsed;
            result.PerformanceMetrics.UsedStrategy = strategy;

            _logger.LogInformation("Completed scan in {Duration}ms using {Strategy} strategy",
                stopwatch.ElapsedMilliseconds, strategy);

            await _messageHandler.SendMessageAsync($"Scan completed in {stopwatch.ElapsedMilliseconds}ms",
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scan for request");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    ///     Internal implementation that returns strongly typed result
    /// </summary>
    public async Task<ScanResult> ScanSingleLogInternalAsync(string logPath,
        CancellationToken cancellationToken = default)
    {
        var request = new ScanRequest
        {
            LogPaths = new[] { logPath },
            EnableCaching = true,
            PerformFullAnalysis = true
        };

        return await ExecuteScanInternalAsync(request, cancellationToken);
    }

    /// <summary>
    ///     Internal implementation that returns strongly typed statistics
    /// </summary>
    public async Task<ScanStatistics> GetStatisticsInternalAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var cacheStats = _cache.GetStatistics();

            return new ScanStatistics
            {
                TotalScansExecuted = 0, // Would be tracked in a production implementation
                AverageProcessingTime = TimeSpan.Zero, // Would be calculated from historical data
                CacheHitRate = 0.0, // Would be calculated from cache statistics
                CurrentStrategy = _currentStrategy,
                CacheStatistics = cacheStats
            };
        }, cancellationToken);
    }

    /// <summary>
    ///     Selects the optimal processing strategy based on workload characteristics
    /// </summary>
    private async Task<ProcessingStrategy> SelectOptimalStrategyAsync(ScanRequest request,
        CancellationToken cancellationToken)
    {
        if (_configuration.PreferredMode != ProcessingMode.Adaptive)
            return ConvertProcessingModeToStrategy(_configuration.PreferredMode);

        return await Task.Run(() =>
        {
            var logCount = request.LogPaths.Count();
            var systemCores = Environment.ProcessorCount;

            // Strategy selection logic
            if (logCount == 1) return ProcessingStrategy.Sequential;

            if (logCount <= systemCores && logCount <= 4) return ProcessingStrategy.Parallel;

            if (logCount > systemCores * 2) return ProcessingStrategy.ProducerConsumer;

            return ProcessingStrategy.Parallel;
        }, cancellationToken);
    }

    /// <summary>
    ///     Executes scan using sequential processing
    /// </summary>
    private async Task<ScanResult> ExecuteSequentialScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var result = new ScanResult
        {
            IsSuccessful = true,
            PerformanceMetrics = { UsedStrategy = ProcessingStrategy.Sequential, ThreadsUsed = 1 }
        };

        var results = new List<ScanResult>();

        foreach (var logPath in request.LogPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var singleResult = await ProcessSingleLogAsync(logPath, cancellationToken);
                results.Add(singleResult);

                await _messageHandler.SendProgressAsync(
                    results.Count,
                    request.LogPaths.Count(),
                    $"Processed {Path.GetFileName(logPath)}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process log: {LogPath}", logPath);
                result.IsSuccessful = false;
                result.ErrorMessage += $"Failed to process {logPath}: {ex.Message}\n";
            }
        }

        // Aggregate results
        result = AggregateResults(results);
        return result;
    }

    /// <summary>
    ///     Executes scan using parallel processing
    /// </summary>
    private async Task<ScanResult> ExecuteParallelScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var result = new ScanResult
        {
            IsSuccessful = true,
            PerformanceMetrics =
                { UsedStrategy = ProcessingStrategy.Parallel, ThreadsUsed = Environment.ProcessorCount }
        };

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _configuration.MaxConcurrentLogs
        };

        var results = new List<ScanResult>();
        var completedCount = 0;

        try
        {
            await Parallel.ForEachAsync(request.LogPaths, parallelOptions, async (logPath, ct) =>
            {
                try
                {
                    var singleResult = await ProcessSingleLogAsync(logPath, ct);

                    lock (results)
                    {
                        results.Add(singleResult);
                        completedCount++;
                    }

                    await _messageHandler.SendProgressAsync(
                        completedCount,
                        request.LogPaths.Count(),
                        $"Processed {Path.GetFileName(logPath)}",
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process log: {LogPath}", logPath);
                    lock (result)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += $"Failed to process {logPath}: {ex.Message}\n";
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parallel processing failed");
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
        }

        // Aggregate results
        result = AggregateResults(results);
        return result;
    }

    /// <summary>
    ///     Executes scan using producer-consumer pattern
    /// </summary>
    private async Task<ScanResult> ExecuteProducerConsumerScanAsync(ScanRequest request,
        CancellationToken cancellationToken)
    {
        var result = new ScanResult
        {
            IsSuccessful = true,
            PerformanceMetrics =
                { UsedStrategy = ProcessingStrategy.ProducerConsumer, ThreadsUsed = _configuration.MaxConcurrentLogs }
        };

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(_configuration.BatchSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        var results = new List<ScanResult>();
        var completedCount = 0;
        var totalCount = request.LogPaths.Count();

        // Producer task
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var logPath in request.LogPaths) await channel.Writer.WriteAsync(logPath, cancellationToken);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        // Consumer tasks
        var consumerTasks = Enumerable.Range(0, _configuration.MaxConcurrentLogs)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var logPath in channel.Reader.ReadAllAsync(cancellationToken))
                    try
                    {
                        var singleResult = await ProcessSingleLogAsync(logPath, cancellationToken);

                        lock (results)
                        {
                            results.Add(singleResult);
                            completedCount++;
                        }

                        await _messageHandler.SendProgressAsync(
                            completedCount,
                            totalCount,
                            $"Processed {Path.GetFileName(logPath)}",
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process log: {LogPath}", logPath);
                        // Note: Error handling simplified for compilation
                    }
            }, cancellationToken))
            .ToArray();

        // Wait for all tasks to complete
        var allTasks = new List<Task>(consumerTasks) { producerTask };
        await Task.WhenAll(allTasks);

        // Aggregate results
        result = AggregateResults(results);
        return result;
    }

    /// <summary>
    ///     Processes a single crash log file
    /// </summary>
    private async Task<ScanResult> ProcessSingleLogAsync(string logPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var fileName = Path.GetFileName(logPath);

        try
        {
            // Check cache first
            var cachedResult = _cache.GetScanResult(logPath);
            if (cachedResult != null)
            {
                _logger.LogDebug("Using cached result for: {FileName}", fileName);
                return cachedResult;
            }

            var result = new ScanResult
            {
                LogFileName = fileName,
                IsSuccessful = true
            };

            // Parse crash log
            var parseStopwatch = Stopwatch.StartNew();
            var crashLog = await _crashLogParser.ParseCrashLogAsync(logPath, cancellationToken);
            parseStopwatch.Stop();
            result.PerformanceMetrics.ParseTime = parseStopwatch.Elapsed;

            // Cache parsed log
            _cache.SetCrashLog(logPath, crashLog);

            // Analysis phase
            var analysisStopwatch = Stopwatch.StartNew();

            // Scan for suspects
            result.DetectedSuspects = await _suspectScanner.ScanForSuspectsAsync(crashLog, cancellationToken);

            // Analyze plugins  
            if (_pluginAnalyzer is PluginAnalyzer pluginAnalyzer)
                result.PluginAnalysis = await pluginAnalyzer.AnalyzePluginsInternalAsync(crashLog, cancellationToken);
            else
                result.PluginAnalysis = new PluginAnalysisResult();

            // Analyze FormIDs (could be expanded for additional analysis)
            await _formIdAnalyzer.AnalyzeFormIDsAsync(crashLog, cancellationToken);

            // Extract system information
            result.SystemAnalysis = crashLog.ExtractSystemSpecs();

            // Generate recommendations
            result.Recommendations = GenerateRecommendations(result);

            analysisStopwatch.Stop();
            result.PerformanceMetrics.AnalysisTime = analysisStopwatch.Elapsed;

            stopwatch.Stop();
            result.ScanDuration = stopwatch.Elapsed;
            result.PerformanceMetrics.TotalProcessingTime = stopwatch.Elapsed;

            // Cache result
            _cache.SetScanResult(logPath, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process single log: {LogPath}", logPath);

            return new ScanResult
            {
                LogFileName = fileName,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ScanDuration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    ///     Aggregates multiple scan results into a single result
    /// </summary>
    private ScanResult AggregateResults(List<ScanResult> results)
    {
        if (!results.Any()) return new ScanResult { IsSuccessful = false, ErrorMessage = "No results to aggregate" };

        var aggregated = new ScanResult
        {
            LogFileName = $"Aggregated ({results.Count} logs)",
            IsSuccessful = results.All(r => r.IsSuccessful),
            ScanDuration = TimeSpan.FromMilliseconds(results.Sum(r => r.ScanDuration.TotalMilliseconds)),
            // Aggregate detected suspects
            DetectedSuspects = results
                .SelectMany(r => r.DetectedSuspects)
                .GroupBy(s => s.Name)
                .Select(g => g.OrderByDescending(s => s.Confidence).First())
                .OrderByDescending(s => s.Severity)
                .ToList(),
            // Aggregate recommendations
            Recommendations = results
                .SelectMany(r => r.Recommendations)
                .GroupBy(r => r.Title)
                .Select(g => g.First())
                .OrderByDescending(r => r.Priority)
                .ToList()
        };

        // Aggregate error messages
        var errors = results.Where(r => !r.IsSuccessful).Select(r => r.ErrorMessage);
        aggregated.ErrorMessage = string.Join("\n", errors);

        return aggregated;
    }

    /// <summary>
    ///     Generates recommendations based on analysis results
    /// </summary>
    private List<RecommendedAction> GenerateRecommendations(ScanResult result)
    {
        var recommendations = new List<RecommendedAction>();

        // Add recommendations based on detected suspects
        foreach (var suspect in result.DetectedSuspects.Take(3)) // Top 3 suspects
            recommendations.Add(new RecommendedAction
            {
                Title = $"Address {suspect.Name}",
                Description = suspect.Description,
                Priority = suspect.Severity,
                Category = "Crash Suspect",
                Steps = suspect.Solutions,
                MoreInfoUrl = suspect.DocumentationUrl
            });

        // Add plugin-related recommendations
        if (result.PluginAnalysis.ExceedsRecommendedLimit)
            recommendations.Add(new RecommendedAction
            {
                Title = "Reduce Plugin Count",
                Description =
                    $"You have {result.PluginAnalysis.TotalPlugins} plugins loaded, which may cause stability issues.",
                Priority = 6,
                Category = "Plugin Management",
                Steps = new List<string>
                {
                    "Consider merging compatible plugins",
                    "Remove unnecessary plugins",
                    "Convert ESP files to ESL format where possible"
                }
            });

        return recommendations.OrderByDescending(r => r.Priority).ToList();
    }

    /// <summary>
    ///     Converts ProcessingMode enum to ProcessingStrategy enum
    /// </summary>
    private ProcessingStrategy ConvertProcessingModeToStrategy(ProcessingMode mode)
    {
        return mode switch
        {
            ProcessingMode.Sequential => ProcessingStrategy.Sequential,
            ProcessingMode.Parallel => ProcessingStrategy.Parallel,
            ProcessingMode.ProducerConsumer => ProcessingStrategy.ProducerConsumer,
            ProcessingMode.Adaptive => ProcessingStrategy.Sequential, // Fallback
            _ => ProcessingStrategy.Sequential
        };
    }
}

/// <summary>
///     Processing strategy enumeration
/// </summary>
/// <summary>
///     Scan request model
/// </summary>
public class ScanRequest
{
    public IEnumerable<string> LogPaths { get; set; } = Enumerable.Empty<string>();
    public bool EnableCaching { get; set; } = true;
    public bool PerformFullAnalysis { get; set; } = true;
    public ProcessingMode? PreferredMode { get; set; }
}

/// <summary>
///     Scan statistics model
/// </summary>
public class ScanStatistics
{
    public int TotalScansExecuted { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public double CacheHitRate { get; set; }
    public ProcessingStrategy CurrentStrategy { get; set; }
    public CacheStatistics CacheStatistics { get; set; } = new();
}
