using System.CommandLine;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Infrastructure.Extensions;
using Classic.ScanLog.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Classic.CLI.Commands;

public class ScanLogsCommand : Command
{
    public ScanLogsCommand() : base("scan-logs", "Scan crash logs for issues")
    {
        var fcxModeOption = new Option<bool>(
            ["--fcx-mode", "-f"],
            getDefaultValue: () => false,
            description: "Enable FCX mode");

        var showFidValuesOption = new Option<bool>(
            ["--show-fid-values", "-fid"],
            getDefaultValue: () => false,
            description: "Show FormID values in reports");

        var statLoggingOption = new Option<bool>(
            ["--stat-logging", "-s"],
            getDefaultValue: () => false,
            description: "Enable statistical logging");

        var moveUnsolvedOption = new Option<bool>(
            ["--move-unsolved", "-m"],
            getDefaultValue: () => false,
            description: "Move unsolved logs to backup location");

        var iniPathOption = new Option<DirectoryInfo?>(
            ["--ini-path", "-i"],
            description: "Path to the INI file directory");

        var scanPathOption = new Option<DirectoryInfo?>(
            ["--scan-path", "-p"],
            description: "Custom path to scan for crash logs");

        var modsPathOption = new Option<DirectoryInfo?>(
            ["--mods-folder-path", "-mod"],
            description: "Path to the mods folder");

        var simplifyLogsOption = new Option<bool>(
            ["--simplify-logs", "-sim"],
            getDefaultValue: () => false,
            description: "Simplify logs (WARNING: May remove important information)");

        var disableProgressOption = new Option<bool>(
            ["--disable-progress", "-np"],
            getDefaultValue: () => false,
            description: "Disable progress bars in CLI mode");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            getDefaultValue: () => false,
            description: "Enable verbose logging");

        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            getDefaultValue: () => false,
            description: "Quiet mode - minimal output");

        AddOption(fcxModeOption);
        AddOption(showFidValuesOption);
        AddOption(statLoggingOption);
        AddOption(moveUnsolvedOption);
        AddOption(iniPathOption);
        AddOption(scanPathOption);
        AddOption(modsPathOption);
        AddOption(simplifyLogsOption);
        AddOption(disableProgressOption);
        AddOption(verboseOption);
        AddOption(quietOption);

        this.SetHandler(async (context) =>
        {
            var fcxMode = context.ParseResult.GetValueForOption(fcxModeOption);
            var showFidValues = context.ParseResult.GetValueForOption(showFidValuesOption);
            var statLogging = context.ParseResult.GetValueForOption(statLoggingOption);
            var moveUnsolved = context.ParseResult.GetValueForOption(moveUnsolvedOption);
            var iniPath = context.ParseResult.GetValueForOption(iniPathOption);
            var scanPath = context.ParseResult.GetValueForOption(scanPathOption);
            var modsPath = context.ParseResult.GetValueForOption(modsPathOption);
            var simplifyLogs = context.ParseResult.GetValueForOption(simplifyLogsOption);
            var disableProgress = context.ParseResult.GetValueForOption(disableProgressOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            await ExecuteAsync(fcxMode, showFidValues, statLogging, moveUnsolved,
                iniPath, scanPath, modsPath, simplifyLogs, disableProgress, verbose, quiet,
                context.GetCancellationToken());
        });
    }

    private async Task ExecuteAsync(
        bool fcxMode,
        bool showFidValues,
        bool statLogging,
        bool moveUnsolved,
        DirectoryInfo? iniPath,
        DirectoryInfo? scanPath,
        DirectoryInfo? modsPath,
        bool simplifyLogs,
        bool disableProgress,
        bool verbose,
        bool quiet,
        CancellationToken cancellationToken)
    {
        // Configure logging
        var logLevel = quiet ? LogEventLevel.Warning : verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/classic-scan-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure services
        var services = new ServiceCollection();

        // Add infrastructure services
        services.AddClassicInfrastructure();

        // Add ScanLog services
        services.AddScanLogServices();

        // Add CLI message handler
        services.AddSingleton<IMessageHandler, CliMessageHandler>(provider =>
            new CliMessageHandler(!disableProgress));

        var serviceProvider = services.BuildServiceProvider();
        var logger = Log.ForContext<ScanLogsCommand>();

        try
        {
            if (!quiet)
            {
                Console.WriteLine("CLASSIC-8 Crash Log Scanner");
                Console.WriteLine("===========================");
                Console.WriteLine();
            }

            // Determine scan directory
            var crashLogsPath = scanPath?.FullName ?? GetDefaultCrashLogsPath();
            if (!Directory.Exists(crashLogsPath))
            {
                logger.Error("Crash logs directory not found: {Path}", crashLogsPath);
                Environment.Exit(1);
                return;
            }

            logger.Information("Scanning directory: {Path}", crashLogsPath);

            // Find crash log files
            var logFiles = Directory.GetFiles(crashLogsPath, "*.log", SearchOption.AllDirectories);
            if (logFiles.Length == 0)
            {
                logger.Warning("No crash log files found in: {Path}", crashLogsPath);
                return;
            }

            logger.Information("Found {Count} crash log files", logFiles.Length);

            // Get services
            var orchestrator = serviceProvider.GetRequiredService<IScanOrchestrator>();
            var messageHandler = serviceProvider.GetRequiredService<IMessageHandler>();

            // Create scan request
            var reportsPath = Path.Combine(crashLogsPath, "Reports");
            var scanRequest = new ScanRequest
            {
                LogFiles = logFiles.ToList(),
                OutputDirectory = reportsPath,
                EnableFcxMode = fcxMode,
                ShowFormIdValues = showFidValues,
                SimplifyLogs = simplifyLogs,
                MoveUnsolvedLogs = moveUnsolved,
                EnableStatisticalLogging = statLogging,
                ModsPath = modsPath?.FullName,
                IniPath = iniPath?.FullName,
                PreferredMode = ProcessingMode.Adaptive,
                MaxConcurrentLogs = Environment.ProcessorCount,
                GenerateDetailedReports = true,
                GenerateSummaryReport = true,
                ContinueOnError = true
            };

            // Validate request
            var validation = orchestrator.ValidateRequest(scanRequest);
            if (!validation.IsValid)
            {
                logger.Error("Invalid scan configuration: {Issues}", validation.GetSummary());
                Environment.Exit(1);
                return;
            }

            if (validation.HasWarnings)
            {
                foreach (var warning in validation.Warnings)
                {
                    logger.Warning("Configuration warning: {Warning}", warning);
                }
            }

            // Execute scan
            messageHandler.SendMessage($"Starting scan of {logFiles.Length} crash logs...", MessageType.Info, MessageTarget.Cli);

            var result = await orchestrator.ExecuteScanAsync(scanRequest, cancellationToken);

            // Display results
            if (!quiet)
            {
                Console.WriteLine();
                Console.WriteLine("Scan Complete!");
                Console.WriteLine("==============");
                Console.WriteLine($"Total logs: {result.TotalLogs}");
                Console.WriteLine($"Successfully scanned: {result.SuccessfulScans}");
                Console.WriteLine($"Failed scans: {result.FailedScans}");
                Console.WriteLine($"Partial scans: {result.PartialScans}");
                Console.WriteLine($"Processing time: {result.ProcessingTime:mm\\:ss}");
                Console.WriteLine($"Success rate: {result.SuccessRate:F1}%");
                Console.WriteLine($"Reports saved to: {reportsPath}");

                if (result.Errors.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Errors encountered:");
                    foreach (var error in result.Errors.Take(5))
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    if (result.Errors.Count > 5)
                    {
                        Console.WriteLine($"  ... and {result.Errors.Count - 5} more");
                    }
                }

                if (result.Summary.RecommendedActions.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Recommendations:");
                    foreach (var recommendation in result.Summary.RecommendedActions)
                    {
                        Console.WriteLine($"  - {recommendation}");
                    }
                }
            }

            Environment.Exit(result.FailedScans > 0 ? 1 : 0);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Scan failed with error");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }


    private static string GetDefaultCrashLogsPath()
    {
        // Try common locations for crash logs
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var possiblePaths = new[]
        {
            Path.Combine(documentsPath, "My Games", "Fallout4", "F4SE"),
            Path.Combine(documentsPath, "My Games", "Skyrim Special Edition", "SKSE"),
            Path.Combine(Directory.GetCurrentDirectory(), "Crash Logs"),
            Path.Combine(Directory.GetCurrentDirectory(), "logs")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
                return path;
        }

        // Default to current directory
        return Directory.GetCurrentDirectory();
    }
}