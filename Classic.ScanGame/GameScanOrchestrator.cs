using Classic.Core.Interfaces;
using Classic.ScanGame.Checkers;
using Serilog;

namespace Classic.ScanGame;

/// <summary>
/// Orchestrates the execution of all game scanning operations.
/// </summary>
public class GameScanOrchestrator
{
    private readonly IXsePluginChecker _xsePluginChecker;
    private readonly ICrashgenChecker _crashgenChecker;
    private readonly IWryeBashChecker _wryeBashChecker;
    private readonly IModIniScanner _modIniScanner;
    private readonly ILogger _logger;

    public GameScanOrchestrator(
        IXsePluginChecker xsePluginChecker,
        ICrashgenChecker crashgenChecker,
        IWryeBashChecker wryeBashChecker,
        IModIniScanner modIniScanner,
        ILogger logger)
    {
        _xsePluginChecker = xsePluginChecker;
        _crashgenChecker = crashgenChecker;
        _wryeBashChecker = wryeBashChecker;
        _modIniScanner = modIniScanner;
        _logger = logger;
    }

    /// <summary>
    /// Executes all game scanning operations.
    /// </summary>
    /// <returns>A comprehensive report of all scan results.</returns>
    public async Task<string> ExecuteFullScanAsync()
    {
        _logger.Information("Starting full game scan");
        var results = new List<string>();

        try
        {
            // Run all scanners in parallel for better performance
            var scanTasks = new List<Task<string>>
            {
                _xsePluginChecker.ScanAsync(),
                _crashgenChecker.ScanAsync(),
                _wryeBashChecker.ScanAsync(),
                _modIniScanner.ScanAsync()
            };

            var scanResults = await Task.WhenAll(scanTasks);
            results.AddRange(scanResults);

            _logger.Information("Full game scan completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during full game scan");
            results.Add("❌ ERROR: Full game scan failed due to an unexpected error\n-----\n");
        }

        return string.Join("\n", results);
    }

    /// <summary>
    /// Executes XSE plugin checking only.
    /// </summary>
    /// <returns>XSE plugin check results.</returns>
    public async Task<string> ExecuteXseCheckAsync()
    {
        _logger.Information("Starting XSE plugin check");
        try
        {
            var result = await _xsePluginChecker.ScanAsync();
            _logger.Information("XSE plugin check completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during XSE plugin check");
            return "❌ ERROR: XSE plugin check failed\n-----\n";
        }
    }

    /// <summary>
    /// Executes crash generator settings check only.
    /// </summary>
    /// <returns>Crash generator settings check results.</returns>
    public async Task<string> ExecuteCrashgenCheckAsync()
    {
        _logger.Information("Starting crash generator settings check");
        try
        {
            var result = await _crashgenChecker.ScanAsync();
            _logger.Information("Crash generator settings check completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during crash generator settings check");
            return "❌ ERROR: Crash generator settings check failed\n-----\n";
        }
    }

    /// <summary>
    /// Executes Wrye Bash report scanning only.
    /// </summary>
    /// <returns>Wrye Bash report scan results.</returns>
    public async Task<string> ExecuteWryeBashCheckAsync()
    {
        _logger.Information("Starting Wrye Bash report scan");
        try
        {
            var result = await _wryeBashChecker.ScanAsync();
            _logger.Information("Wrye Bash report scan completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during Wrye Bash report scan");
            return "❌ ERROR: Wrye Bash report scan failed\n-----\n";
        }
    }

    /// <summary>
    /// Executes mod INI scanning only.
    /// </summary>
    /// <returns>Mod INI scan results.</returns>
    public async Task<string> ExecuteModIniScanAsync()
    {
        _logger.Information("Starting mod INI scan");
        try
        {
            var result = await _modIniScanner.ScanAsync();
            _logger.Information("Mod INI scan completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during mod INI scan");
            return "❌ ERROR: Mod INI scan failed\n-----\n";
        }
    }

    /// <summary>
    /// Gets the status of all scanners.
    /// </summary>
    /// <returns>A status report of all scanning components.</returns>
    public async Task<string> GetScannerStatusAsync()
    {
        _logger.Information("Getting scanner status");
        var statusReport = new List<string>
        {
            "🔍 GAME SCANNER STATUS REPORT",
            "==============================",
            ""
        };

        try
        {
            // Test each scanner's basic functionality
            var xseStatus = await TestScannerAsync("XSE Plugin Checker", () => _xsePluginChecker.ScanAsync());
            var crashgenStatus = await TestScannerAsync("Crash Generator Checker", () => _crashgenChecker.ScanAsync());
            var wryeStatus = await TestScannerAsync("Wrye Bash Checker", () => _wryeBashChecker.ScanAsync());
            var modIniStatus = await TestScannerAsync("Mod INI Scanner", () => _modIniScanner.ScanAsync());

            statusReport.AddRange(new[]
            {
                $"XSE Plugin Checker: {xseStatus}",
                $"Crash Generator Checker: {crashgenStatus}",
                $"Wrye Bash Checker: {wryeStatus}",
                $"Mod INI Scanner: {modIniStatus}",
                "",
                "=============================="
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting scanner status");
            statusReport.Add("❌ ERROR: Failed to get scanner status");
        }

        return string.Join("\n", statusReport);
    }

    /// <summary>
    /// Tests a scanner's basic functionality.
    /// </summary>
    /// <param name="scannerName">The name of the scanner being tested.</param>
    /// <param name="scannerFunc">The scanner function to test.</param>
    /// <returns>The status of the scanner.</returns>
    private async Task<string> TestScannerAsync(string scannerName, Func<Task<string>> scannerFunc)
    {
        try
        {
            var result = await scannerFunc();
            return result.Contains("ERROR") ? "❌ ERROR" : "✅ READY";
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Scanner {ScannerName} failed status test", scannerName);
            return "❌ ERROR";
        }
    }
}