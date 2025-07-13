using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for monitoring Papyrus logs in real-time.
/// </summary>
public class PapyrusMonitoringService : IPapyrusMonitoringService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _monitoringCancellation;
    private Task? _monitoringTask;
    private PapyrusStats? _lastStats;

    // Regex patterns for parsing Papyrus log content
    private static readonly Regex DumpsRegex = new(@"Dumping Stacks", RegexOptions.Compiled);
    private static readonly Regex StacksRegex = new(@"Dumping Stack", RegexOptions.Compiled);

    private static readonly Regex WarningsRegex =
        new(@"\s+warning:\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ErrorsRegex = new(@"\s+error:\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public event EventHandler<PapyrusStats>? StatsUpdated;
    public event EventHandler<string>? MonitoringError;

    public bool IsMonitoring => _monitoringTask is { IsCompleted: false };

    public PapyrusMonitoringService(ILogger logger, ISettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public async Task<PapyrusStats> GetCurrentStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var papyrusLogPath = GetPapyrusLogPath();

            if (string.IsNullOrEmpty(papyrusLogPath) || !File.Exists(papyrusLogPath))
            {
                return new PapyrusStats
                {
                    Timestamp = DateTime.Now,
                    LogFileExists = false,
                    ErrorMessage = "Papyrus log file not found. Enable Papyrus logging in the game settings."
                };
            }

            var stats = await ParsePapyrusLogAsync(papyrusLogPath, cancellationToken).ConfigureAwait(false);
            _logger.Debug(
                "Retrieved current Papyrus stats: {Dumps} dumps, {Stacks} stacks, {Warnings} warnings, {Errors} errors",
                stats.Dumps, stats.Stacks, stats.Warnings, stats.Errors);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get current Papyrus stats");
            return new PapyrusStats
            {
                Timestamp = DateTime.Now,
                LogFileExists = false,
                ErrorMessage = $"Error reading Papyrus log: {ex.Message}"
            };
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (IsMonitoring)
        {
            _logger.Warning("Papyrus monitoring is already running");
            return;
        }

        _logger.Information("Starting Papyrus log monitoring");

        _monitoringCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitoringTask = MonitoringLoopAsync(_monitoringCancellation.Token);

        await Task.Yield(); // Allow the monitoring task to start
    }

    public async Task StopMonitoringAsync()
    {
        if (!IsMonitoring)
        {
            return;
        }

        _logger.Information("Stopping Papyrus log monitoring");

        _monitoringCancellation?.Cancel();

        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _monitoringTask = null;

        _logger.Information("Papyrus log monitoring stopped");
    }

    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentStats = await GetCurrentStatsAsync(cancellationToken).ConfigureAwait(false);

                    // Only emit if stats have changed
                    if (_lastStats == null || !_lastStats.Equals(currentStats))
                    {
                        _lastStats = currentStats;
                        StatsUpdated?.Invoke(this, currentStats);
                    }

                    // Wait for the next check (1 second interval)
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during Papyrus monitoring loop");
                    MonitoringError?.Invoke(this, ex.Message);

                    // Wait a bit before retrying
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fatal error in Papyrus monitoring loop");
            MonitoringError?.Invoke(this, $"Fatal monitoring error: {ex.Message}");
        }
    }

    private async Task<PapyrusStats> ParsePapyrusLogAsync(string logPath, CancellationToken cancellationToken)
    {
        try
        {
            string content;

            // Read the file content with proper encoding detection
            using var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8, true);
            content = await reader.ReadToEndAsync().ConfigureAwait(false);

            // Count occurrences using regex
            var dumpMatches = DumpsRegex.Matches(content);
            var stackMatches = StacksRegex.Matches(content);
            var warningMatches = WarningsRegex.Matches(content);
            var errorMatches = ErrorsRegex.Matches(content);

            var dumps = dumpMatches.Count;
            var stacks = stackMatches.Count;
            var warnings = warningMatches.Count;
            var errors = errorMatches.Count;

            // Calculate ratio (avoid division by zero)
            var ratio = stacks > 0 ? (double)dumps / stacks : 0.0;

            return new PapyrusStats
            {
                Timestamp = DateTime.Now,
                Dumps = dumps,
                Stacks = stacks,
                Warnings = warnings,
                Errors = errors,
                Ratio = ratio,
                LogFileExists = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to parse Papyrus log at {LogPath}", logPath);
            throw;
        }
    }

    private string? GetPapyrusLogPath()
    {
        try
        {
            // Try to get the Papyrus log path from settings
            // This would need to be implemented based on your settings structure
            var settings = _settingsService.Settings;

            // For now, return a placeholder - this should be implemented based on your game configuration
            // The Python code references: yaml_settings(Path, YAML.Game_Local, f"Game{GlobalRegistry.get_vr()}_Info.Docs_File_PapyrusLog")

            // This is a common Papyrus log location for Fallout 4
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var papyrusLogPath = Path.Combine(documentsPath, "My Games", "Fallout4", "Logs", "Script",
                "Papyrus.0.log");

            return File.Exists(papyrusLogPath) ? papyrusLogPath : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to determine Papyrus log path");
            return null;
        }
    }

    public void Dispose()
    {
        StopMonitoringAsync().GetAwaiter().GetResult();
        _monitoringCancellation?.Dispose();
    }
}
