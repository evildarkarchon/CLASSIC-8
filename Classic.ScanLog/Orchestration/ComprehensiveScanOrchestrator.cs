using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Core.Enums;
using Classic.ScanLog.Analyzers;
using Classic.ScanLog.Utilities;
using Classic.ScanLog.Validators;
using Classic.ScanLog.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace Classic.ScanLog.Orchestration;

/// <summary>
/// Comprehensive implementation of IScanOrchestrator with full feature support
/// </summary>
public class ComprehensiveScanOrchestrator : IScanOrchestrator
{
    private readonly ICrashLogParser _parser;
    private readonly IPluginAnalyzer _pluginAnalyzer;
    private readonly IFormIdAnalyzer _formIdAnalyzer;
    private readonly EnhancedSuspectScanner _suspectScanner;
    private readonly ModConflictDetector _modConflictDetector;
    private readonly GameFileValidator _gameFileValidator;
    private readonly IReportGenerator _reportGenerator;
    private readonly IMessageHandler _messageHandler;
    private readonly CrashLogReformatter _reformatter;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComprehensiveScanOrchestrator> _logger;

    // Enhanced async processing services
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ResourceManager _resourceManager;
    private readonly AdaptiveProcessingEngine _adaptiveEngine;

    private readonly Dictionary<ProcessingMode, Func<ScanRequest, CancellationToken, Task<ScanResult>>> _strategies;
    private readonly PerformanceMetrics _performanceMetrics = new();

    public ComprehensiveScanOrchestrator(
        ICrashLogParser parser,
        IPluginAnalyzer pluginAnalyzer,
        IFormIdAnalyzer formIdAnalyzer,
        EnhancedSuspectScanner suspectScanner,
        ModConflictDetector modConflictDetector,
        GameFileValidator gameFileValidator,
        IReportGenerator reportGenerator,
        IMessageHandler messageHandler,
        CrashLogReformatter reformatter,
        IServiceProvider serviceProvider,
        ILogger<ComprehensiveScanOrchestrator> logger,
        PerformanceMonitor performanceMonitor,
        ResourceManager resourceManager,
        AdaptiveProcessingEngine adaptiveEngine)
    {
        _parser = parser;
        _pluginAnalyzer = pluginAnalyzer;
        _formIdAnalyzer = formIdAnalyzer;
        _suspectScanner = suspectScanner;
        _modConflictDetector = modConflictDetector;
        _gameFileValidator = gameFileValidator;
        _reportGenerator = reportGenerator;
        _messageHandler = messageHandler;
        _reformatter = reformatter;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _resourceManager = resourceManager;
        _adaptiveEngine = adaptiveEngine;

        _strategies = new Dictionary<ProcessingMode, Func<ScanRequest, CancellationToken, Task<ScanResult>>>
        {
            { ProcessingMode.Sequential, ExecuteSequentialAsync },
            { ProcessingMode.Parallel, ExecuteParallelAsync },
            { ProcessingMode.ProducerConsumer, ExecuteProducerConsumerAsync },
            { ProcessingMode.Adaptive, ExecuteAdaptiveAsync }
        };
    }

    public async Task<ScanResult> ExecuteScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        // Validate request
        var validation = ValidateRequest(request);
        if (!validation.IsValid) throw new ArgumentException($"Invalid scan request: {validation.GetSummary()}");

        // Initialize result
        var result = new ScanResult
        {
            StartTime = DateTime.Now,
            TotalLogs = request.LogFiles.Count,
            OutputDirectory = request.OutputDirectory,
            OriginalRequest = request
        };

        try
        {
            _logger.LogInformation("Starting scan of {Count} crash logs using {Mode} processing",
                request.LogFiles.Count, request.PreferredMode);

            // Send initial progress
            await _messageHandler.SendMessageAsync(
                $"Starting scan of {request.LogFiles.Count} crash logs...", cancellationToken);

            // Ensure output directory exists
            Directory.CreateDirectory(request.OutputDirectory);

            // Reformat crash logs if requested
            if (request.ReformatLogs) await ReformatCrashLogsAsync(request, cancellationToken);

            // Determine optimal processing strategy using adaptive engine
            var processingMode = request.PreferredMode == ProcessingMode.Adaptive
                ? await _adaptiveEngine.GetOptimalProcessingModeAsync(request, cancellationToken)
                : request.PreferredMode;

            result.UsedProcessingMode = processingMode;

            // Calculate optimal worker count and batch size
            var optimalWorkers = _adaptiveEngine.CalculateOptimalWorkerCount(processingMode, request);
            var optimalBatchSize = _adaptiveEngine.CalculateOptimalBatchSize(processingMode, request);

            // Update request with optimal values
            request.MaxConcurrentLogs = optimalWorkers;
            request.BatchSize = optimalBatchSize;
            result.WorkersUsed = optimalWorkers;

            // Execute using the selected strategy with performance monitoring
            if (_strategies.TryGetValue(processingMode, out var strategy))
            {
                var strategyStopwatch = Stopwatch.StartNew();
                var strategyResult = await strategy(request, cancellationToken);
                strategyStopwatch.Stop();

                // Record performance data for adaptive engine
                var performanceData = new ProcessingPerformanceData
                {
                    ProcessingTime = strategyStopwatch.Elapsed,
                    FilesProcessed = strategyResult.SuccessfulScans + strategyResult.PartialScans,
                    TotalFiles = request.LogFiles.Count,
                    ErrorCount = strategyResult.FailedScans,
                    PeakMemoryUsage = _performanceMonitor.GetStatisticsAsync().Result.PeakMemoryUsage,
                    AverageCpuUsage = _performanceMonitor.GetStatisticsAsync().Result.AverageCpuUsage,
                    Context = new ProcessingDecisionContext
                    {
                        FileCount = request.LogFiles.Count,
                        MemoryUsagePercent = _resourceManager.GetResourceUsage().MemoryUsagePercent,
                        SystemLoad = _performanceMonitor.GetStatisticsAsync().Result.CpuUsagePercent / 100.0
                    }
                };

                _adaptiveEngine.RecordPerformanceData(processingMode, performanceData);

                // Merge results
                result.SuccessfulScans = strategyResult.SuccessfulScans;
                result.FailedScans = strategyResult.FailedScans;
                result.PartialScans = strategyResult.PartialScans;
                result.DetailedResults = strategyResult.DetailedResults;
                result.Errors = strategyResult.Errors;
                result.Warnings = strategyResult.Warnings;
                result.UnsolvedLogs = strategyResult.UnsolvedLogs;
                result.ProcessedLogs = strategyResult.ProcessedLogs;
                result.GameDistribution = strategyResult.GameDistribution;
                result.ModConflicts = strategyResult.ModConflicts;
            }
            else
            {
                throw new NotSupportedException($"Processing mode {processingMode} is not supported");
            }

            // Generate reports
            if (request.GenerateDetailedReports || request.GenerateSummaryReport)
                await GenerateReportsAsync(result, request, cancellationToken);

            // Move unsolved logs if requested
            if (request.MoveUnsolvedLogs && result.UnsolvedLogs.Count > 0)
                await MoveUnsolvedLogsAsync(result, request, cancellationToken);

            // Generate summary
            result.Summary = GenerateScanSummary(result);

            result.EndTime = DateTime.Now;
            result.Performance = _performanceMetrics;

            _logger.LogInformation(
                "Scan completed: {Successful}/{Total} successful, {Failed} failed in {Duration:mm\\:ss}",
                result.SuccessfulScans, result.TotalLogs, result.FailedScans, result.ProcessingTime);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.Now;
            result.AddError($"Scan failed: {ex.Message}");
            _logger.LogError(ex, "Scan execution failed");
            throw;
        }
    }

    public async Task<ScanLogResult> ScanSingleLogAsync(string logPath, ScanRequest? config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ScanLogResult
        {
            LogPath = logPath,
            ProcessingTime = DateTime.Now
        };

        try
        {
            // Parse the crash log
            var crashLog = await _parser.ParseCrashLogAsync(logPath, cancellationToken);

            // Extract basic information
            result.GameVersion = crashLog.GameVersion;
            result.CrashGenVersion = crashLog.CrashGenVersion;
            result.CrashDate = crashLog.DateCreated;
            result.MainError = crashLog.MainError;

            // Run analyzers if configured
            if (config?.EnablePluginAnalysis == true)
            {
                var pluginStopwatch = Stopwatch.StartNew();
                await _pluginAnalyzer.AnalyzePluginsAsync(crashLog, cancellationToken);
                result.AnalyzerTimes["PluginAnalyzer"] = pluginStopwatch.Elapsed;
                // Plugin count would be determined from analysis results
            }

            if (config?.EnableFormIdAnalysis == true)
            {
                var formIdStopwatch = Stopwatch.StartNew();
                await _formIdAnalyzer.AnalyzeFormIDsAsync(crashLog, cancellationToken);
                result.AnalyzerTimes["FormIdAnalyzer"] = formIdStopwatch.Elapsed;
                // FormID count would be determined from analysis results
            }

            if (config?.EnableSuspectScanning == true)
            {
                var suspectStopwatch = Stopwatch.StartNew();
                var suspects = await _suspectScanner.ScanForSuspectsAsync(crashLog, cancellationToken);
                result.AnalyzerTimes["SuspectScanner"] = suspectStopwatch.Elapsed;

                // Store suspects in the result
                result.Suspects.AddRange(suspects.Select(s => s.Name));

                _logger.LogDebug("Found {Count} suspects in {LogPath}", suspects.Count, logPath);
            }

            // Mod conflict detection
            var modConflictStopwatch = Stopwatch.StartNew();
            var crashLogData = CreateCrashLogData(crashLog);
            var modConflicts = await _modConflictDetector.DetectModConflictsAsync(crashLogData);
            result.AnalyzerTimes["ModConflictDetector"] = modConflictStopwatch.Elapsed;

            // Store mod conflicts in the result
            result.ModConflicts.AddRange(modConflicts.Select(mc => $"[{mc.Type}] {mc.ModName}: {mc.Warning}"));

            _logger.LogDebug("Found {Count} mod conflicts in {LogPath}", modConflicts.Count, logPath);

            // Additional analyzers would go here...

            result.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to scan log: {LogPath}", logPath);
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    public async Task<ScanLogResult> ScanSingleLogAsync(string logPath, CancellationToken cancellationToken = default)
    {
        return await ScanSingleLogAsync(logPath, null, cancellationToken);
    }

    public async Task<PerformanceMetrics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_performanceMetrics);
    }

    public ValidationResult ValidateRequest(ScanRequest request)
    {
        return request.Validate();
    }

    public async Task<ProcessingMode> GetOptimalProcessingModeAsync(ScanRequest request,
        CancellationToken cancellationToken = default)
    {
        // Delegate to adaptive engine for optimal mode selection
        return await _adaptiveEngine.GetOptimalProcessingModeAsync(request, cancellationToken);
    }

    public async Task<TimeSpan> EstimateProcessingTimeAsync(ScanRequest request,
        CancellationToken cancellationToken = default)
    {
        // Rough estimation based on file count and processing mode
        var baseTimePerFile = TimeSpan.FromSeconds(2); // Base estimate per file
        var totalTime = TimeSpan.FromMilliseconds(request.LogFiles.Count * baseTimePerFile.TotalMilliseconds);

        // Adjust for processing mode
        var mode = request.PreferredMode == ProcessingMode.Adaptive
            ? await GetOptimalProcessingModeAsync(request, cancellationToken)
            : request.PreferredMode;

        return mode switch
        {
            ProcessingMode.Sequential => totalTime,
            ProcessingMode.Parallel => TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds /
                                                                 Math.Min(request.MaxConcurrentLogs,
                                                                     Environment.ProcessorCount)),
            ProcessingMode.ProducerConsumer => TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds /
                                                                         (request.MaxConcurrentLogs * 0.8)),
            ProcessingMode.Adaptive => TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds /
                                                                 (request.MaxConcurrentLogs * 0.9)),
            _ => totalTime
        };
    }

    private async Task<ScanResult> ExecuteSequentialAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var result = new ScanResult();

        for (var i = 0; i < request.LogFiles.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var logFile = request.LogFiles[i];
            _messageHandler.ReportProgress("Sequential scan", i + 1, request.LogFiles.Count);

            try
            {
                var logResult = await ScanSingleLogAsync(logFile, request, cancellationToken);
                result.AddLogResult(logResult);
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to process {Path.GetFileName(logFile)}: {ex.Message}", logFile);
                if (!request.ContinueOnError)
                    throw;
            }
        }

        return result;
    }

    private async Task<ScanResult> ExecuteParallelAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var semaphore = new SemaphoreSlim(request.MaxConcurrentLogs);
        var processedCount = 0;

        var tasks = request.LogFiles.Select(async logFile =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var logResult = await ScanSingleLogAsync(logFile, request, cancellationToken);

                lock (result)
                {
                    result.AddLogResult(logResult);
                    processedCount++;
                    _messageHandler.ReportProgress("Parallel scan", processedCount, request.LogFiles.Count);
                }
            }
            catch (Exception ex)
            {
                lock (result)
                {
                    result.AddError($"Failed to process {Path.GetFileName(logFile)}: {ex.Message}", logFile);
                }

                if (!request.ContinueOnError)
                    throw;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return result;
    }

    private async Task<ScanResult> ExecuteProducerConsumerAsync(ScanRequest request,
        CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var channel = Channel.CreateBounded<string>(request.BatchSize);
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Producer task
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var logFile in request.LogFiles) await writer.WriteAsync(logFile, cancellationToken);
            }
            finally
            {
                writer.Complete();
            }
        }, cancellationToken);

        // Consumer tasks
        var consumerTasks = Enumerable.Range(0, request.MaxConcurrentLogs)
            .Select(_ => Task.Run(async () =>
            {
                var processedCount = 0;
                await foreach (var logFile in reader.ReadAllAsync(cancellationToken))
                    try
                    {
                        var logResult = await ScanSingleLogAsync(logFile, request, cancellationToken);

                        lock (result)
                        {
                            result.AddLogResult(logResult);
                            processedCount++;
                            _messageHandler.ReportProgress("Producer-Consumer scan", processedCount,
                                request.LogFiles.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (result)
                        {
                            result.AddError($"Failed to process {Path.GetFileName(logFile)}: {ex.Message}", logFile);
                        }

                        if (!request.ContinueOnError)
                            throw;
                    }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(new[] { producerTask }.Concat(consumerTasks));
        return result;
    }

    private async Task<ScanResult> ExecuteAdaptiveAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        // Enhanced adaptive processing with real-time monitoring
        var optimalMode = await _adaptiveEngine.GetOptimalProcessingModeAsync(request, cancellationToken);
        var result = new ScanResult();

        // Start with optimal mode
        var currentMode = optimalMode;
        var processedFiles = 0;
        var remainingFiles = new List<string>(request.LogFiles);

        while (remainingFiles.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            // Create batch request
            var batchSize = _adaptiveEngine.CalculateOptimalBatchSize(currentMode, request);
            var batchFiles = remainingFiles.Take(batchSize).ToList();
            remainingFiles = remainingFiles.Skip(batchSize).ToList();

            var batchRequest = new ScanRequest
            {
                LogFiles = batchFiles,
                OutputDirectory = request.OutputDirectory,
                PreferredMode = currentMode,
                MaxConcurrentLogs = _adaptiveEngine.CalculateOptimalWorkerCount(currentMode, request),
                BatchSize = batchSize,
                // Copy other settings
                EnablePluginAnalysis = request.EnablePluginAnalysis,
                EnableFormIdAnalysis = request.EnableFormIdAnalysis,
                EnableSuspectScanning = request.EnableSuspectScanning,
                EnableModDetection = request.EnableModDetection,
                ContinueOnError = request.ContinueOnError
            };

            // Process batch
            var batchStopwatch = Stopwatch.StartNew();
            var batchResult = await _strategies[currentMode](batchRequest, cancellationToken);
            batchStopwatch.Stop();

            // Create performance data for this batch
            var batchPerformanceData = new ProcessingPerformanceData
            {
                ProcessingTime = batchStopwatch.Elapsed,
                FilesProcessed = batchResult.SuccessfulScans + batchResult.PartialScans,
                TotalFiles = batchFiles.Count,
                ErrorCount = batchResult.FailedScans,
                PeakMemoryUsage = _performanceMonitor.GetStatisticsAsync().Result.PeakMemoryUsage,
                AverageCpuUsage = _performanceMonitor.GetStatisticsAsync().Result.AverageCpuUsage,
                Context = new ProcessingDecisionContext
                {
                    FileCount = batchFiles.Count,
                    MemoryUsagePercent = _resourceManager.GetResourceUsage().MemoryUsagePercent,
                    SystemLoad = _performanceMonitor.GetStatisticsAsync().Result.CpuUsagePercent / 100.0
                }
            };

            // Check for adaptation recommendations
            var adaptation =
                await _adaptiveEngine.MonitorAndAdaptAsync(currentMode, batchPerformanceData, cancellationToken);
            if (adaptation != null && adaptation.Confidence > 0.7)
            {
                _logger.LogInformation("Adapting processing mode from {CurrentMode} to {SuggestedMode}: {Reason}",
                    currentMode, adaptation.SuggestedMode, adaptation.Reason);

                currentMode = adaptation.SuggestedMode;
                batchRequest.MaxConcurrentLogs = adaptation.SuggestedWorkerCount;
                batchRequest.BatchSize = adaptation.SuggestedBatchSize;
            }

            // Merge batch results
            result.SuccessfulScans += batchResult.SuccessfulScans;
            result.FailedScans += batchResult.FailedScans;
            result.PartialScans += batchResult.PartialScans;
            result.DetailedResults.AddRange(batchResult.DetailedResults);
            result.Errors.AddRange(batchResult.Errors);
            result.Warnings.AddRange(batchResult.Warnings);
            result.UnsolvedLogs.AddRange(batchResult.UnsolvedLogs);
            result.ProcessedLogs.AddRange(batchResult.ProcessedLogs);

            foreach (var gameDistribution in batchResult.GameDistribution)
            {
                result.GameDistribution.TryGetValue(gameDistribution.Key, out var count);
                result.GameDistribution[gameDistribution.Key] = count + gameDistribution.Value;
            }

            foreach (var modConflict in batchResult.ModConflicts)
            {
                result.ModConflicts.TryGetValue(modConflict.Key, out var count);
                result.ModConflicts[modConflict.Key] = count + modConflict.Value;
            }

            processedFiles += batchFiles.Count;

            // Report progress
            _messageHandler.ReportProgress("Adaptive scan", processedFiles, request.LogFiles.Count);

            // Record performance data
            _adaptiveEngine.RecordPerformanceData(currentMode, batchPerformanceData);
        }

        return result;
    }

    private async Task GenerateReportsAsync(ScanResult result, ScanRequest request, CancellationToken cancellationToken)
    {
        if (request.GenerateSummaryReport)
        {
            var summaryPath =
                Path.Combine(request.OutputDirectory, $"ScanSummary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.md");
            await File.WriteAllTextAsync(summaryPath, result.GenerateTextSummary(), cancellationToken);
            result.SummaryReportPath = summaryPath;
            result.GeneratedReports.Add(summaryPath);
        }

        // Generate individual reports for each log if requested
        if (request.GenerateDetailedReports)
            foreach (var logResult in result.DetailedResults.Where(r => r.IsSuccessful))
            {
                var reportPath = Path.Combine(request.OutputDirectory,
                    Path.GetFileNameWithoutExtension(logResult.LogPath) + "-DETAILED.md");

                await GenerateDetailedReportAsync(logResult, reportPath, cancellationToken);
                logResult.ReportPath = reportPath;
                result.GeneratedReports.Add(reportPath);
            }
    }

    private async Task GenerateDetailedReportAsync(ScanLogResult logResult, string reportPath,
        CancellationToken cancellationToken)
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("# Detailed Crash Log Analysis Report");
        report.AppendLine($"**File:** {logResult.LogFileName}");
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("## Basic Information");
        report.AppendLine($"- Game: {logResult.GameId}");
        report.AppendLine($"- Game Version: {logResult.GameVersion}");
        report.AppendLine($"- CrashGen Version: {logResult.CrashGenVersion}");
        report.AppendLine($"- Crash Date: {logResult.CrashDate}");
        report.AppendLine($"- Processing Time: {logResult.Duration.TotalSeconds:F2}s");
        report.AppendLine();

        if (!string.IsNullOrEmpty(logResult.MainError))
        {
            report.AppendLine("## Main Error");
            report.AppendLine($"```");
            report.AppendLine(logResult.MainError);
            report.AppendLine($"```");
            report.AppendLine();
        }

        if (logResult.IdentifiedMods.Count > 0)
        {
            report.AppendLine("## Identified Mods");
            foreach (var mod in logResult.IdentifiedMods) report.AppendLine($"- {mod}");
            report.AppendLine();
        }

        if (logResult.Suspects.Count > 0)
        {
            report.AppendLine("## Suspects");
            foreach (var suspect in logResult.Suspects) report.AppendLine($"- {suspect}");
            report.AppendLine();
        }

        if (logResult.ModConflicts.Count > 0)
        {
            report.AppendLine("## Mod Conflicts");
            foreach (var conflict in logResult.ModConflicts) report.AppendLine($"- {conflict}");
            report.AppendLine();
        }

        await File.WriteAllTextAsync(reportPath, report.ToString(), cancellationToken);
    }

    private async Task MoveUnsolvedLogsAsync(ScanResult result, ScanRequest request,
        CancellationToken cancellationToken)
    {
        var backupPath = request.BackupPath ?? Path.Combine(request.OutputDirectory, "Unsolved");
        Directory.CreateDirectory(backupPath);

        foreach (var unsolvedLog in result.UnsolvedLogs)
            try
            {
                var fileName = Path.GetFileName(unsolvedLog);
                var destPath = Path.Combine(backupPath, fileName);
                File.Move(unsolvedLog, destPath, true);
                result.MovedLogs.Add(destPath);
            }
            catch (Exception ex)
            {
                result.AddWarning($"Failed to move unsolved log {unsolvedLog}: {ex.Message}");
            }
    }

    private ScanSummary GenerateScanSummary(ScanResult result)
    {
        var summary = new ScanSummary();

        // Analyze error patterns
        foreach (var error in result.Errors)
        {
            var category = CategorizeError(error);
            summary.ErrorCategories.TryGetValue(category, out var count);
            summary.ErrorCategories[category] = count + 1;
        }

        // Get top mod conflicts
        summary.TopModConflicts = result.ModConflicts
            .OrderByDescending(x => x.Value)
            .Take(10)
            .Select(x => x.Key)
            .ToList();

        // Generate recommendations
        summary.RecommendedActions = GenerateRecommendations(result);

        // Overall assessment
        if (result.SuccessRate > 90)
            summary.OverallAssessment = "Excellent - Most logs processed successfully";
        else if (result.SuccessRate > 70)
            summary.OverallAssessment = "Good - Majority of logs processed successfully";
        else if (result.SuccessRate > 50)
            summary.OverallAssessment = "Fair - Some issues encountered";
        else
            summary.OverallAssessment = "Poor - Significant issues found";

        return summary;
    }

    private string CategorizeError(string error)
    {
        if (error.Contains("permission", StringComparison.OrdinalIgnoreCase))
            return "Permission Issues";
        if (error.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
            return "Corrupted Files";
        if (error.Contains("format", StringComparison.OrdinalIgnoreCase))
            return "Format Issues";
        if (error.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "Timeout Issues";

        return "Other";
    }

    private List<string> GenerateRecommendations(ScanResult result)
    {
        var recommendations = new List<string>();

        if (result.FailureRate > 20) recommendations.Add("High failure rate detected - check for corrupted log files");

        if (result.ModConflicts.Count > 10) recommendations.Add("Multiple mod conflicts found - review mod load order");

        if (result.ProcessingTime.TotalMinutes > 10)
            recommendations.Add("Long processing time - consider using parallel processing mode");

        return recommendations;
    }

    /// <summary>
    /// Reformats crash log files according to configuration settings
    /// </summary>
    private async Task ReformatCrashLogsAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting crash log reformatting for {Count} files", request.LogFiles.Count);

            await _messageHandler.SendMessageAsync("Reformatting crash logs...", cancellationToken);

            // Get remove patterns from configuration (default patterns for simplification)
            var removePatterns = new List<string>
            {
                "XINPUT1_3.dll",
                "steam_api64.dll",
                "gameoverlayrenderer64.dll",
                "d3d11.dll",
                "d3d9.dll"
            };

            await _reformatter.ReformatCrashLogsAsync(
                request.LogFiles,
                removePatterns,
                request.SimplifyLogs,
                cancellationToken);

            _logger.LogDebug("Completed crash log reformatting");

            await _messageHandler.SendMessageAsync("Crash log reformatting completed", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reformat crash logs");

            // Don't fail the entire scan if reformatting fails
            await _messageHandler.SendMessageAsync($"Warning: Reformatting failed - {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Creates CrashLogData from parsed CrashLog for mod conflict detection
    /// </summary>
    private static CrashLogData CreateCrashLogData(CrashLog crashLog)
    {
        return new CrashLogData
        {
            MainError = crashLog.MainError,
            SystemSpecs = crashLog.Segments.TryGetValue("SystemSpecs", out var systemSpecs)
                ? string.Join("\n", systemSpecs)
                : null,
            CallStack = crashLog.Segments.TryGetValue("CallStack", out var callStack)
                ? string.Join("\n", callStack)
                : null,
            Plugins = crashLog.Plugins,
            Headers = new Dictionary<string, object>
            {
                ["GameVersion"] = crashLog.GameVersion,
                ["CrashGenVersion"] = crashLog.CrashGenVersion,
                ["FileName"] = crashLog.FileName
            }
        };
    }
}
